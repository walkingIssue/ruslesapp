using System.Security.Claims;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;

namespace rustles_platform.RateLimiting;

public static class RustlesRateLimiting
{
    public static IServiceCollection AddRustlesRateLimiting(
        this IServiceCollection services,
        bool isDevelopment)
    {
        services.AddRateLimiter(options => Configure(options, isDevelopment));

        return services;
    }

    public static PartitionedRateLimiter<HttpContext> CreateGlobalLimiter(bool isDevelopment)
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(
            context => GetPartition(context, isDevelopment));
    }

    private static void Configure(RateLimiterOptions options, bool isDevelopment)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            TimeSpan? retryAfter = null;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
            {
                retryAfter = retry;
                context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retry.TotalSeconds).ToString();
            }

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded.",
                retryAfterSeconds = retryAfter is null ? (int?)null : (int)Math.Ceiling(retryAfter.Value.TotalSeconds)
            }, cancellationToken: token);
        };

        options.GlobalLimiter = CreateGlobalLimiter(isDevelopment);
    }

    private static RateLimitPartition<string> GetPartition(HttpContext context, bool isDevelopment)
    {
        var path = context.Request.Path;
        var method = context.Request.Method;
        var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!HttpMethods.IsPost(method))
        {
            return RateLimitPartition.GetNoLimiter("non-mutating-requests");
        }

        if (path.StartsWithSegments("/Account/Register"))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"register:{remoteIp}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = isDevelopment ? 100 : 4,
                    Window = isDevelopment ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(15),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        if (path.StartsWithSegments("/Account/Login")
            || path.StartsWithSegments("/Account/LoginWith2fa")
            || path.StartsWithSegments("/Account/LoginWithRecoveryCode"))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"login:{remoteIp}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = isDevelopment ? 200 : 10,
                    Window = isDevelopment ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        if (path.StartsWithSegments("/Account"))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"account-post:{remoteIp}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = isDevelopment ? 300 : 20,
                    Window = isDevelopment ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        if (path.StartsWithSegments("/api/ConnectMunip"))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"connect-munip:{actor}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 8,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        if (path.StartsWithSegments("/api/fbb0a13f-2a4a-477c-84a5-e5f18232b00e"))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"trusted-tester:{actor}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 12,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            $"default:{remoteIp}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 240,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }
}
