using Microsoft.AspNetCore.SignalR.Client;

using rustles.contracts;

namespace rustles_platform.Client.Pages.Municipality
{
    public static class MunipConnectionExtensions
    {

        public static Task MunipChat(this HubConnection connection, MunipMessageRequest request)
        {
            return connection.InvokeAsync(nameof(IMunipServerMethods.MunipUserMessage), request);
        }

        public static Task ReactToMessage(this HubConnection connection, MunipReactionRequest request)
        {
            return connection.InvokeAsync(nameof(IMunipServerMethods.ReactToMessage), request);
        }
    }
}
