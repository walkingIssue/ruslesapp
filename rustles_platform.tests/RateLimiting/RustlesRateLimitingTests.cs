using System.Net;

using Microsoft.AspNetCore.Http;

using rustles_platform.RateLimiting;

namespace rustles_platform.tests.RateLimiting;

public sealed class RustlesRateLimitingTests
{
    [Fact]
    public async Task NonPostRequests_are_not_rate_limited()
    {
        using var limiter = RustlesRateLimiting.CreateGlobalLimiter(isDevelopment: false);
        var context = CreateContext(HttpMethods.Get, "/appsettings.json");

        for (var i = 0; i < 500; i++)
        {
            using var lease = await limiter.AcquireAsync(context);

            Assert.True(lease.IsAcquired);
        }
    }

    [Fact]
    public async Task LoginPosts_are_rate_limited_in_production()
    {
        using var limiter = RustlesRateLimiting.CreateGlobalLimiter(isDevelopment: false);
        var context = CreateContext(HttpMethods.Post, "/Account/Login");

        for (var i = 0; i < 10; i++)
        {
            using var lease = await limiter.AcquireAsync(context);

            Assert.True(lease.IsAcquired);
        }

        using var rejectedLease = await limiter.AcquireAsync(context);

        Assert.False(rejectedLease.IsAcquired);
    }

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        return context;
    }
}
