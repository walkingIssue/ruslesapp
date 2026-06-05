using Moq;

using rustles.contracts;
using rustles_platform.Client.Pages.Municipality.DTOs;
using rustles_platform.Client.Pages.Municipality.Services.Abstractions;
using rustles_platform.Client.Pages.Municipality.Services.Api;
using rustles_platform.Client.Pages.Municipality.Services.ChatSession;
using rustles_platform.Client.Pages.Municipality.Services.Geolocation;

namespace rustles_platform.tests.Services;

public sealed class ChatOrchestratorTests
{
    [Fact]
    public async Task AssignMunipHandlers_can_run_after_queue_success_before_hub_start()
    {
        var geolocation = new Mock<IGeolocation>(MockBehavior.Strict);
        var geoApi = new Mock<IRustlesGeoApi>(MockBehavior.Strict);
        var session = new Mock<IChatSession>(MockBehavior.Strict);
        var sequence = new MockSequence();

        geolocation
            .Setup(item => item.AssertUserGeolocationConsent(
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                true))
            .ReturnsAsync(new UserConsent
            {
                consented = true,
                latti = 59.3293,
                longi = 18.0686
            });

        geoApi
            .Setup(item => item.EnqueWithRetry(
                It.IsAny<Position>(),
                It.IsAny<Action<Progress>>()))
            .ReturnsAsync(new RustlesGeoResponse(APIRESPONSECODE.OK, null));

        session
            .InSequence(sequence)
            .Setup(item => item.RegisterHandler(
                It.IsAny<Func<MunipChatMessage, Task>>(),
                It.IsAny<Func<ConnectMunipAck, Task>>(),
                It.IsAny<Func<MunipReactionUpdate, Task>>(),
                It.IsAny<Func<MunipPinnedMessages, Task>>(),
                It.IsAny<Func<MunipMessageDeleted, Task>>(),
                It.IsAny<Func<MunipChatError, Task>>()));

        session
            .InSequence(sequence)
            .Setup(item => item.InitializeUserSessionAsync(It.IsAny<RustlesGeoResponse>()))
            .Returns(Task.CompletedTask);

        var orchestrator = new ChatOrchestrator(
            geolocation.Object,
            geoApi.Object,
            session.Object);

        var location = await orchestrator.RequestClientLocation();
        var queue = await orchestrator.EnqueClient(_ => Task.CompletedTask, Retry: true);

        orchestrator.AssignMunipHandlers(
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask);
        var connection = await orchestrator.ConnectHub();

        Assert.True(location.ContextValue.Resolvable);
        Assert.True(queue.ContextValue.Enqued);
        Assert.True(connection.ContextValue);
        session.VerifyAll();
    }
}
