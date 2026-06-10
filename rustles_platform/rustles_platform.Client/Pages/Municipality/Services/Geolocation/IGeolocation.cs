using Microsoft.JSInterop;

using rustles_platform.Client.Pages.Municipality.DTOs;

namespace rustles_platform.Client.Pages.Municipality.Services.Geolocation
{
    public interface IGeolocation
    {
        public ValueTask<UserConsent?> AssertUserGeolocationConsent(TimeSpan userWaitInterval, TimeSpan cachedAge, bool highAccurazy);
    }

    internal interface RustlesGeo
    {
        ValueTask<UserConsent> assertGeolocationCompliance();
    }
    public class Geolocation : IGeolocation
    {
        readonly IJSRuntime _runtime;
        readonly ILogger<Geolocation> _logger;
        private const string ClientGeoLocationApiMethod = "RustlesGeo.assertGeolocationCompliance";
        public Geolocation(IJSRuntime runtime, ILogger<Geolocation> logger)
        {
            _runtime = runtime;
            _logger = logger;
        }
        public async ValueTask<UserConsent?> AssertUserGeolocationConsent(TimeSpan deviceWaitInterval, TimeSpan cachedAge, bool highAccurazy)
        {
            try
            {
                return await _runtime.InvokeAsync<UserConsent?>(ClientGeoLocationApiMethod, [deviceWaitInterval.Seconds, cachedAge.Seconds, highAccurazy]);
            } catch (Exception ex) 
            {
                _logger.LogError("Compliance Timed out {0}", ex);
                return null;
            }
        }
    }
}
