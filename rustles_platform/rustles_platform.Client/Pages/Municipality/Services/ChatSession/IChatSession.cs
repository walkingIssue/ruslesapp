using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

using rustles.contracts;

using rustles_platform.Client.Pages.Municipality.Services.Api;

using System.Runtime.CompilerServices;

namespace rustles_platform.Client.Pages.Municipality.Services.ChatSession
{
    public enum ErrorType { Chat, Connection, SignIn, ClientGeoDenied, ServerGeoDenied }
    public enum DisconnectedReason { Network, RateLimited, PositionInvalid }
    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectedReason Reason { get; set; }
        public DisconnectedEventArgs(DisconnectedReason reason) : base()
        {
            Reason = reason;
        }
    }

    public class ReconnectingEventArgs : EventArgs
    {
        public TimeSpan TimeLapsed { get; set; }
        public int Attempts { get; set; }
        public const int MaxAttempts = 4;
        readonly TimeSpan MaxTimeBeforeDisconnect = TimeSpan.FromSeconds(32);
        public ReconnectingEventArgs(TimeSpan timelapsed)
        {
            TimeLapsed = timelapsed;
        }
    }

    public interface IChatSession : IAsyncDisposable
    {
        //Intializes the hub
        public Task InitializeUserSessionAsync(RustlesGeoResponse response);

        public void RegisterHandler(
            Func<MunipChatMessage, Task> OnMessge, 
            Func<ConnectMunipAck, Task> OnConnect,
            Func<MunipReactionUpdate, Task> OnReaction,
            Func<MunipPinnedMessages, Task> OnPinnedMessages,
            Func<MunipMessageDeleted, Task> OnMessageDeleted,
            Func<MunipChatError, Task> error);

        public Task SendMessageAsync(MunipMessageRequest request);
        public Task ReactToMessageAsync(MunipReactionRequest request);

        public delegate void DisconnectedHandler(DisconnectedEventArgs eventArgs);
        public delegate void ReconnectedHandler();
        public delegate void ReconnectingHandler(ReconnectingEventArgs eventArgs);

        public event DisconnectedHandler? OnDisconnected;
        public event ReconnectedHandler? OnReconnected;
        public event ReconnectingHandler? Reconnecting;
    }


    public class MunipSession : IChatSession
    {
        private HubConnection? _con;
        private readonly IHubConnectionBuilder _builder;
        private const string munpHubUrl = "/hubs/municipality";
        public MunipSession(HubConnectionBuilder builder, NavigationManager manager)
        {
            _builder = builder.WithUrl(manager.ToAbsoluteUri(munpHubUrl));
        }

        public event IChatSession.DisconnectedHandler? OnDisconnected;
        public event IChatSession.ReconnectedHandler? OnReconnected;
        public event IChatSession.ReconnectingHandler? Reconnecting;

        public ValueTask DisposeAsync()
        {
            if(_con != null)
                return _con.DisposeAsync();

            return ValueTask.CompletedTask;
        }

        public Task InitializeUserSessionAsync(RustlesGeoResponse response)
        {
            if (response.code != APIRESPONSECODE.OK)
                throw new ArgumentException($"{nameof(response)} is not currently sucessfull, cannot intialize builder!");

            var connection = EnsureConnection();
            return connection.State == HubConnectionState.Disconnected
                ? connection.StartAsync()
                : Task.CompletedTask;
        }

        private HubConnection EnsureConnection()
        {
            if (_con is not null)
                return _con;

            _con = _builder
                .WithKeepAliveInterval(TimeSpan.FromSeconds(15))
                .WithAutomaticReconnect()
                .Build();

            _con.Closed += _con_Closed;
            _con.Reconnected += _con_Reconnected;
            _con.Reconnecting += _con_Reconnecting;

            return _con;
        }

        private TimeOnly? disconnected;
        private Task _con_Reconnecting(Exception? arg)
        {
            if(disconnected.HasValue)
                this.Reconnecting?.Invoke(new ReconnectingEventArgs(TimeOnly.FromDateTime(DateTime.UtcNow) - disconnected.Value));
            return Task.CompletedTask;
        }

        private Task _con_Reconnected(string? arg)
        {
            this.disconnected = null;
            this.OnReconnected?.Invoke();
            return Task.CompletedTask;
        }
        
        //eventually we might create our own exception times and send propper diconnection reasons to the UI
        private Task _con_Closed(Exception? arg)
        {
            disconnected = TimeOnly.FromDateTime(DateTime.UtcNow);
                this.OnDisconnected?.Invoke(new DisconnectedEventArgs(DisconnectedReason.Network));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="OnMessge"></param>
        /// <param name="OnConnect"></param>
        /// <param name="error"></param>
        /// <exception cref="Exception">Throws if the builder hasn't been called with a successfull response</exception>
        public void RegisterHandler(
            Func<MunipChatMessage, Task> OnMessge,
            Func<ConnectMunipAck, Task> OnConnect,
            Func<MunipReactionUpdate, Task> OnReaction,
            Func<MunipPinnedMessages, Task> OnPinnedMessages,
            Func<MunipMessageDeleted, Task> OnMessageDeleted,
            Func<MunipChatError, Task> error)
        {
            var connection = EnsureConnection();

            connection.On<MunipChatMessage>(nameof(IMunipClientMethods.MessageByUser), OnMessge);
            connection.On<ConnectMunipAck>(nameof(IMunipClientMethods.MunipConnect),  OnConnect);
            connection.On<MunipReactionUpdate>(nameof(IMunipClientMethods.ReactionUpdated), OnReaction);
            connection.On<MunipPinnedMessages>(nameof(IMunipClientMethods.PinnedMessagesUpdated), OnPinnedMessages);
            connection.On<MunipMessageDeleted>(nameof(IMunipClientMethods.MessageDeleted), OnMessageDeleted);
            connection.On<MunipChatError>(nameof(IMunipClientMethods.ConnectionError), error);
        }

        public Task SendMessageAsync(MunipMessageRequest request)
        {
            if (_con is not null && request is not null && !string.IsNullOrWhiteSpace(request.Message))
                return _con.MunipChat(request);

            throw new Exception("Connection to Hub is not established!");
        }

        public Task ReactToMessageAsync(MunipReactionRequest request)
        {
            if (_con is not null && request is not null && !string.IsNullOrWhiteSpace(request.Emoji))
                return _con.ReactToMessage(request);

            throw new Exception("Connection to Hub is not established!");
        }

    }
}
