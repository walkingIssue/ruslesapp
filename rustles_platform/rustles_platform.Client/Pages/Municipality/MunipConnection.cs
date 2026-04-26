using Microsoft.AspNetCore.SignalR.Client;

using rustles.contracts;

namespace rustles_platform.Client.Pages.Municipality
{
    public static class MunipConnectionExtensions
    {

        public static Task MunipChat(this HubConnection connection, string message)
        {
            return connection.InvokeAsync(nameof(IMunipServerMethods.MunipUserMessage), message);
        }
    }
}
