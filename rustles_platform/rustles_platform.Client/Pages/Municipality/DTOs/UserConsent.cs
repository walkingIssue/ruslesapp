namespace rustles_platform.Client.Pages.Municipality.DTOs
{
    //shadow of the return value from assertGeolocationCompliance method for interop to deserialize js return value into C#
    public class UserConsent
    { 
        public Boolean consented { get; set; }
        public double? latti { get; set; }
        public double? longi { get; set; }
        public string? errorMessage { get; set; }
    }
}
