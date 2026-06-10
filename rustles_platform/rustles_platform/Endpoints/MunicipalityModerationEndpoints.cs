using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using rustles.contracts;
using rustles.Infrastructure.Model;
using rustles_platform.Hubs;
using rustles_platform.Services;

namespace rustles_platform.Endpoints;

public static class MunicipalityModerationEndpoints
{
    public static IEndpointRouteBuilder MapMunicipalityModerationEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder
            .MapGroup("/api/municipality-chat/moderation")
            .RequireAuthorization(policy =>
            {
                policy.RequireRole("TrustedTester");
            });

        group.MapPost("/messages/{messageId:guid}/delete", DeleteMessageAsync)
            .WithRequestTimeout(TimeSpan.FromSeconds(30));

        group.MapPost("/messages/{messageId:guid}/ban-author", BanMessageAuthorAsync)
            .WithRequestTimeout(TimeSpan.FromSeconds(30));

        return builder;
    }

    private static async Task<IResult> DeleteMessageAsync(
        Guid messageId,
        [FromBody] ModerationRequest request,
        HttpContext context,
        [FromServices] IMunicipalityChatStore chatStore,
        [FromServices] IHubContext<MunicipalityHub, IMunipClientMethods> hubContext,
        CancellationToken cancellationToken)
    {
        var moderatorUserId = GetUserId(context);
        if (moderatorUserId is null)
            return TypedResults.Unauthorized();

        var result = await chatStore.DeleteMessageAsync(
            messageId,
            moderatorUserId,
            request.Reason,
            cancellationToken);

        if (result is null)
            return TypedResults.NotFound();

        await hubContext.Clients.Group(result.MunicipalityName)
            .MessageDeleted(new MunipMessageDeleted(result.MessageId));

        var pinnedMessages = await chatStore.GetPinnedMessagesAsync(result.MunicipalityName, cancellationToken);
        await hubContext.Clients.Group(result.MunicipalityName)
            .PinnedMessagesUpdated(new MunipPinnedMessages(pinnedMessages));

        return TypedResults.Ok(new ModerationResponse(true, "Message deleted."));
    }

    private static async Task<IResult> BanMessageAuthorAsync(
        Guid messageId,
        [FromBody] ModerationRequest request,
        HttpContext context,
        [FromServices] IMunicipalityChatStore chatStore,
        CancellationToken cancellationToken)
    {
        var moderatorUserId = GetUserId(context);
        if (moderatorUserId is null)
            return TypedResults.Unauthorized();

        var result = await chatStore.BanMessageAuthorAsync(
            messageId,
            moderatorUserId,
            request.Reason,
            request.ExpiresAt,
            cancellationToken);

        return result is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new ModerationResponse(true, "Message author banned."));
    }

    private static string? GetUserId(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public sealed record ModerationRequest(string? Reason, DateTimeOffset? ExpiresAt = null);

    public sealed record ModerationResponse(bool Succeeded, string Message);
}
