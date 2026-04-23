
namespace rustles.contracts
{

    public sealed record MunipConnectionRequest(int MunicipalityId);

    public sealed record MunipChatMessage(
        string SenderId,
        string DisplayName,
        string Message,
        DateTimeOffset SentAt);

    public sealed record ConnectMunipAck(
        string UserName,
        string Message,
        IReadOnlyList<MunipChatMessage> Catchup
        );

    public sealed record MunipMessageRequest(string Message);

    public sealed record ConnectMunipRequest(int MunipId);

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
        Task<ConnectMunipAck?> ConnectUser(ConnectMunipRequest request);
        Task MunipUserMessage(string message);
    }
    
    /// <summary>
    /// Methods available on the client
    /// </summary>
    public interface IMunipClientMethods
    {
        Task MunipConnect(ConnectMunipAck ack);
        Task MessageByUser(MunipChatMessage message);
        Task ConnectionError(MunipChatError error);
    }
}
