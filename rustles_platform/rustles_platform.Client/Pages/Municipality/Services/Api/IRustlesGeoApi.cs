using Microsoft.AspNetCore.Components.Forms;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace rustles_platform.Client.Pages.Municipality.Services.Api
{
    public enum APIRESPONSECODE { OK, FAIL, MALFORMED, HIGHLOAD, RETRY, UNAUTHORIZED, RATELIMITED, USERBANNED, INTERNALERROR, NETWORKERROR }
    public record RustlesGeoResponse(APIRESPONSECODE code, TimeSpan? retryInterval);
    public record Position(double latti, double longi);
    public record LoadFail(int extension, TimeSpan? recommended);
    public enum Progress { Retry30, Retry60, Retry120, Failed }
    public interface IRustlesGeoApi
    {
        public Task<RustlesGeoResponse> EnqueUserPosition(Position position);
        public Task<RustlesGeoResponse> EnqueWithRetry(Position position, Action<Progress> action);
    }

    public class RustlesGeoApi : IRustlesGeoApi
    {
        readonly HttpClient _client;
        readonly AntiforgeryStateProvider _stateprov;
        readonly ILogger<RustlesGeoApi> _logger;
        private const string VerifyHeader = "RequestVerificationToken";
        private const string apiPath = "api/ConnectMunip";
        private TimeSpan _highLoadSpan = TimeSpan.FromSeconds(30);

        public RustlesGeoApi(HttpClient client, AntiforgeryStateProvider provider, ILogger<RustlesGeoApi> logger)
        {
            _client = client;
            _stateprov = provider;
            _logger = logger;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <returns>OK -> enqued; 
        ///         Unauthorized -> user not signed in, or doesnt exist in db; 
        ///         Highload -> Call retry; 
        ///         Malformed -> null values; 
        ///         Ratelimited -> retry in 5-10 mins;
        ///         InternalError -> something is very fishy, shouldnt happen;
        ///         NetworkError -> no internet?
        ///         </returns>
        public async Task<RustlesGeoResponse> EnqueUserPosition(Position position)
        {
            try
            {
                var result = await _client.SendAsync(CreateRequestMessage(position));
                if (result.IsSuccessStatusCode)
                    return new RustlesGeoResponse(APIRESPONSECODE.OK, null);

                if(result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return new RustlesGeoResponse(APIRESPONSECODE.UNAUTHORIZED, null);

                if (result.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    var loadFail = await result.Content.ReadFromJsonAsync<LoadFail>();
                    return new RustlesGeoResponse(APIRESPONSECODE.HIGHLOAD, loadFail?.recommended);
                }

                if (result.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                    return new RustlesGeoResponse(APIRESPONSECODE.MALFORMED, null);

                return new RustlesGeoResponse(APIRESPONSECODE.RATELIMITED, null);
            }
            catch (NullReferenceException ex) 
            {
                _logger.LogError("Request caused API to not respond with body {0}", ex);
                return new RustlesGeoResponse(APIRESPONSECODE.INTERNALERROR, null);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Failed to contact Rustles Servers {0}", ex);
                return new RustlesGeoResponse(APIRESPONSECODE.NETWORKERROR, null);
            }
        }

        private HttpRequestMessage CreateRequestMessage(Position position)
        {
            var requestMessage = new HttpRequestMessage();
            requestMessage.Method = HttpMethod.Post;
            requestMessage.RequestUri = new Uri(apiPath, UriKind.Relative);
            var token = _stateprov.GetAntiforgeryToken();
            if (!string.IsNullOrWhiteSpace(token?.Value))
                requestMessage.Headers.Add(VerifyHeader, token.Value);
            requestMessage.Content = JsonContent.Create(position);
            return requestMessage;
        }

        public async Task<RustlesGeoResponse> EnqueWithRetry(Position position, Action<Progress> progressCallback)
        {
            var result = await EnqueUserPosition(position);

            if (result.code == APIRESPONSECODE.USERBANNED
                || result.code == APIRESPONSECODE.MALFORMED
                || result.code == APIRESPONSECODE.INTERNALERROR
                || result.code == APIRESPONSECODE.RATELIMITED)
                return result;

            RustlesGeoResponse response;
            if(result.code == APIRESPONSECODE.HIGHLOAD)
            {
                return await HighLoadHandle(position, progressCallback, 0, _highLoadSpan);
            }
            if(result.code == APIRESPONSECODE.RETRY)
            {
                progressCallback(Progress.Retry30);
                await Task.Delay(result.retryInterval ?? _highLoadSpan);
                response = await EnqueUserPosition(position);
                if (response.code != APIRESPONSECODE.OK)
                    return response;
            }
            return new RustlesGeoResponse(APIRESPONSECODE.OK, TimeSpan.FromMinutes(30));
        }

        private async Task<RustlesGeoResponse> HighLoadHandle(Position position, Action<Progress> callback, int stage, TimeSpan span)
        {

            RustlesGeoResponse response;
            callback((Progress)stage);
            await Task.Delay(span);
            response = await EnqueUserPosition(position);
            if (response.code == APIRESPONSECODE.HIGHLOAD && stage < 4)
                response = await HighLoadHandle(position, callback, ++stage, span * stage);
            else if (response.code == APIRESPONSECODE.OK)
                return response;
            return new RustlesGeoResponse(APIRESPONSECODE.RATELIMITED, TimeSpan.FromMinutes(30));
        }
    }
}
