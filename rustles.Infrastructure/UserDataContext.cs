using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using rustles.Infrastructure.Model;

namespace rustles.Infrastructure;

public class UsersDataContext : IdentityDbContext<User>
{
    public UsersDataContext(DbContextOptions<UsersDataContext> options) : base(options)
    {
    }

    public DbSet<UserAgeSelfAttestation> UserAgeSelfAttestations { get; set; }
    public DbSet<UserCookiePolicyConsent> UserCookiePolicyConsents { get; set; }
    public DbSet<PlatformPolicy> PlatformPolicies { get; set; }
    public DbSet<UserPolicyAcceptance> UserPolicyAcceptances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlatformPolicy>()
            .HasIndex(policy => new { policy.PolicyKey, policy.Version })
            .IsUnique();

        modelBuilder.Entity<UserPolicyAcceptance>()
            .HasIndex(acceptance => new { acceptance.UserId, acceptance.PlatformPolicyId })
            .IsUnique();
    }
}
