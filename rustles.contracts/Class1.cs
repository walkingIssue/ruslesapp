
namespace rustles.contracts
{

    public sealed record MunipReactionSummary(string Emoji, int Count);

    public sealed record MunipChatMessage(
        Guid MessageId,
        string DisplayName,
        string Message,
        DateTimeOffset SentAt,
        Guid? ReplyToMessageId = null,
        string? ReplyToDisplayName = null,
        string? ReplyToPreview = null,
        IReadOnlyList<MunipReactionSummary>? Reactions = null,
        bool IsPinned = false);

    public sealed record ConnectMunipAck(
        string MunipName,
        string UserName,
        string Message,
        IReadOnlyList<MunipChatMessage> Catchup,
        IReadOnlyList<MunipChatMessage>? PinnedMessages = null
        );

    public sealed record MunipMessageRequest(string Message, Guid? ReplyToMessageId = null);

    public sealed record MunipReactionRequest(Guid MessageId, string Emoji);

    public sealed record MunipReactionUpdate(Guid MessageId, IReadOnlyList<MunipReactionSummary> Reactions);

    public sealed record MunipPinnedMessages(IReadOnlyList<MunipChatMessage> Messages);

    public sealed record MunipMessageDeleted(Guid MessageId);

    public sealed record MunipChatError(ConnectionFault Fault, ChatErrorMessage ErrorMessage);
    
    public enum ActionResult { ok, fault}

    public sealed record ClientAction<T>(ActionResult Result, ConnectionFault? Fault, T? ResponseObject);

    /// <summary>
    /// Rough sketch, i'll figure it out eventually
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ResponseObject"></param>
    public sealed record MunipChatRequestResult<T>(T? ResponseObject)
    {
        private readonly ClientAction<T> _ok = new(ActionResult.ok, null, ResponseObject);
        public ClientAction<T> Ok => _ok;
        public ClientAction<T> ResolutionFail => new(ActionResult.fault, ConnectionFault.fail, ResponseObject);
    }

    public enum ChatErrorMessage { error = 0, clientDenied = 1, length = 2, forbiddenSymbols = 4}

    /// <summary>
    /// methods available on the server
    /// </summary>
    public interface IMunipServerMethods
    {
        Task MunipUserMessage(MunipMessageRequest request);
        Task ReactToMessage(MunipReactionRequest request);
    }
    
    /// <summary>
    /// Methods available on the client
    /// </summary>
    public interface IMunipClientMethods
    {
        Task MunipConnect(ConnectMunipAck ack);
        Task MessageByUser(MunipChatMessage message);
        Task ReactionUpdated(MunipReactionUpdate update);
        Task PinnedMessagesUpdated(MunipPinnedMessages pinnedMessages);
        Task MessageDeleted(MunipMessageDeleted deletion);
        Task ConnectionError(MunipChatError error);
    }
}
