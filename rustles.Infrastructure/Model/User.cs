

using Microsoft.AspNetCore.Identity;

using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace rustles.Infrastructure.Model;


public class User : IdentityUser
{
    public string PermanentUsername { get; set; } = string.Empty;
    public UserAgeSelfAttestation? AgeAttest { get; set; }
    public ICollection<UserCookiePolicyConsent?> UserCookiePolicyConsen { get; set; } = [];
    public ICollection<UserPolicyAcceptance?> UserPolicyAcceptances { get; set; } = [];

}

