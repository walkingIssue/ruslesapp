using rustles.contracts;

using rustles_platform.Client.Pages.Municipality.Services.Api;
using rustles_platform.Client.Pages.Municipality.Services.ChatSession;
using rustles_platform.Client.Pages.Municipality.Services.Geolocation;

namespace rustles_platform.Client.Pages.Municipality.Services.Abstractions
{
    [Flags]
    public enum ServiceState { Success =0, Error =1, Loading=2, Retry = 4,
        ClientGeo = 8, RustlesApi = 16, RustlesHub = 32, Disconnected = 64 }


    public record ServiceResponse<T>(ServiceState? Systemstate, T ContextValue);
    public record Consent(bool Consented, bool Resolvable);
    public enum ApiResolution { Ok, Unauthorized, Highload, Malformed, Ratelimited, InternalError, NetworkError, Fail}
    public record ApiResponse(bool Enqued, ApiResolution Resolution);

    public interface IChatOrchestrator : IAsyncDisposable
    {
        Task<ServiceResponse<Consent>> RequestClientLocation();
        Task<ServiceResponse<ApiResponse>> EnqueClient(Func<Progress, Task>? progressReport, bool Retry = false);
        Task<ServiceResponse<bool>> ConnectHub();

        Task SendMessage(string message, Guid? replyToMessageId = null);
        Task ReactToMessage(Guid messageId, string emoji);

        public void AssignMunipHandlers(
            Func<MunipChatMessage, Task> recieveMessageHandler,
            Func<ConnectMunipAck, Task> receiveCatchupHandler,
            Func<MunipReactionUpdate, Task> reactionHandler,
            Func<MunipPinnedMessages, Task> pinnedMessagesHandler,
            Func<MunipMessageDeleted, Task> messageDeletedHandler,
            Func<MunipChatError, Task> chatErrorHandler);
        public void SubscribeToDisconnect(Action<DisconnectedEventArgs> Action);
        public void SubscribeToReconnecting(Action<ReconnectingEventArgs> Action);
        public void SubscribeToReconnected(Action Reconnected);
        public void UnsubscribeAll();
    }

    public class ChatOrchestrator : IChatOrchestrator
    {
        private bool lastApiEnqueu = false;
        private Position? latestClientPos;
        private bool MunipConnected = false;

        private readonly IGeolocation _clientGeo;
        private readonly IRustlesGeoApi _geoApi;
        private readonly IChatSession _session;

        public ChatOrchestrator(
            IGeolocation clientGeo, 
            IRustlesGeoApi serverGeo, 
            IChatSession session)
        {
            _clientGeo = clientGeo;
            _geoApi = serverGeo;
            _session = session;
        }

        private ApiResolution ResolutionTransformer(APIRESPONSECODE code)
        {
            return code switch
            {
                APIRESPONSECODE.OK => ApiResolution.Ok,
                APIRESPONSECODE.UNAUTHORIZED => ApiResolution.Unauthorized,
                APIRESPONSECODE.HIGHLOAD => ApiResolution.Highload,
                APIRESPONSECODE.MALFORMED => ApiResolution.Malformed,
                APIRESPONSECODE.RATELIMITED => ApiResolution.Ratelimited,
                APIRESPONSECODE.INTERNALERROR => ApiResolution.InternalError,
                APIRESPONSECODE.NETWORKERROR => ApiResolution.NetworkError,
                _ => ApiResolution.Fail
            };
        }

        public void AssignMunipHandlers(
            Func<MunipChatMessage, Task> recieveMessageHandler,
            Func<ConnectMunipAck, Task> receiveCatchupHandler,
            Func<MunipReactionUpdate, Task> reactionHandler,
            Func<MunipPinnedMessages, Task> pinnedMessagesHandler,
            Func<MunipMessageDeleted, Task> messageDeletedHandler,
            Func<MunipChatError, Task> chatErrorHandler)
        {
            if (lastApiEnqueu is false)
                throw new InvalidOperationException("Municipality handlers cannot be assigned before the API queue succeeds.");

            _session.RegisterHandler(
                recieveMessageHandler,
                receiveCatchupHandler,
                reactionHandler,
                pinnedMessagesHandler,
                messageDeletedHandler,
                chatErrorHandler);
        }

        public async Task<ServiceResponse<bool>> ConnectHub()
        {
            if (lastApiEnqueu is false)
                return new(ServiceState.Error | ServiceState.Retry | ServiceState.RustlesApi, false);
            if (latestClientPos is null)
                return new(ServiceState.Error | ServiceState.Retry | ServiceState.ClientGeo, false);

            await _session.InitializeUserSessionAsync(new RustlesGeoResponse(APIRESPONSECODE.OK, null));
            MunipConnected = true;
            return new(ServiceState.Success | ServiceState.RustlesHub, true);
        }

        public async Task<ServiceResponse<ApiResponse>> EnqueClient(Func<Progress, Task>? progressReport, bool Retry = false)
        {
            if (latestClientPos == null)
                return new ServiceResponse<ApiResponse>(ServiceState.Error | ServiceState.Retry | ServiceState.ClientGeo, new(false, ApiResolution.Fail));
            
            if(progressReport is null && Retry)
                throw new ArgumentNullException(nameof(progressReport));

            if(!Retry && progressReport is null)
            {
                var result = await _geoApi.EnqueUserPosition(latestClientPos);
                if (result.code != APIRESPONSECODE.OK)
                    return new(ServiceState.Error | ServiceState.RustlesApi, new(false, ResolutionTransformer(result.code)));
            }

            if (Retry && progressReport is not null)
            {
                var result = await _geoApi.EnqueWithRetry(latestClientPos, async x => await progressReport(x));
                if (result.code != APIRESPONSECODE.OK)
                    return new(ServiceState.Error | ServiceState.RustlesApi, new ApiResponse(false, ResolutionTransformer(result.code)));
            }

            lastApiEnqueu = true;
            return new(ServiceState.Success | ServiceState.Loading | ServiceState.RustlesApi, new(true, ApiResolution.Ok));
        }

        public async Task<ServiceResponse<Consent>> RequestClientLocation()
        {
            var result = await _clientGeo.AssertUserGeolocationConsent(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), true);

            if (result is null)
                return new ServiceResponse<Consent>(ServiceState.Error | ServiceState.ClientGeo | ServiceState.Disconnected, new Consent(false, false));

            if (!result.consented)
                return new ServiceResponse<Consent>(ServiceState.Error | ServiceState.ClientGeo, new Consent(false, false));

            if (result.consented && !result.latti.HasValue && !result.longi.HasValue)
                return new(ServiceState.Error | ServiceState.ClientGeo, new(true, false));

            this.latestClientPos = new(result.latti!.Value, result.longi!.Value);

            return new(ServiceState.Success | ServiceState.ClientGeo, new(true, true));
        }



        public Task SendMessage(string message, Guid? replyToMessageId = null)
        {
            if (MunipConnected == true && _session is not null)
                return _session.SendMessageAsync(new MunipMessageRequest(message, replyToMessageId));

            throw new Exception("Currently Disconnected!");
        }

        public Task ReactToMessage(Guid messageId, string emoji)
        {
            if (MunipConnected == true && _session is not null)
                return _session.ReactToMessageAsync(new MunipReactionRequest(messageId, emoji));

            throw new Exception("Currently Disconnected!");
        }

        IChatSession.DisconnectedHandler? disconnected;
        IChatSession.ReconnectingHandler? recon;
        IChatSession.ReconnectedHandler? reconnected;

        public void SubscribeToDisconnect(Action<DisconnectedEventArgs> Action)
        {
            _session.OnDisconnected += (disconnected = new IChatSession.DisconnectedHandler(Action));
        }
        public void SubscribeToReconnected(Action Reconnected)
        {
            _session.OnReconnected += (reconnected = new IChatSession.ReconnectedHandler(Reconnected));
        }

        public void SubscribeToReconnecting(Action<ReconnectingEventArgs> Action)
        {
            _session.Reconnecting += (recon = new IChatSession.ReconnectingHandler(Action));
        }

        public void UnsubscribeAll()
        {
            if (disconnected is not null)
                _session.OnDisconnected -= disconnected;
            if (recon is not null)
                _session.Reconnecting -= recon;
            if (reconnected is not null)
                _session.OnReconnected -= reconnected;
        }

        public async ValueTask DisposeAsync()
        {
            this.lastApiEnqueu = false;
            this.latestClientPos = null;
            this.MunipConnected = false;
            this.UnsubscribeAll();
            if(_session != null) 
            await _session.DisposeAsync();
        }
    }
}
