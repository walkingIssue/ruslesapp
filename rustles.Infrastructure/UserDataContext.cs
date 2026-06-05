using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using rustles.Infrastructure.Model;

namespace rustles.Infrastructure;

public class UsersDataContext : IdentityDbContext<User>
{
    public UsersDataContext(DbContextOptions<UsersDataContext> options) : base(options)
    {
    }

    public DbSet<MunicipalityChatMessage> MunicipalityChatMessages { get; set; }
    public DbSet<MunicipalityMessageReaction> MunicipalityMessageReactions { get; set; }
    public DbSet<MunicipalityUserBan> MunicipalityUserBans { get; set; }
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

        modelBuilder.Entity<MunicipalityChatMessage>()
            .HasIndex(message => new { message.MunicipalityName, message.SentAt });

        modelBuilder.Entity<MunicipalityChatMessage>()
            .HasOne(message => message.User)
            .WithMany(user => user.MunicipalityChatMessages)
            .HasForeignKey(message => message.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MunicipalityChatMessage>()
            .HasOne(message => message.ReplyToMessage)
            .WithMany(message => message.Replies)
            .HasForeignKey(message => message.ReplyToMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MunicipalityMessageReaction>()
            .HasIndex(reaction => new { reaction.MunicipalityChatMessageId, reaction.Emoji, reaction.ReactedAt });

        modelBuilder.Entity<MunicipalityMessageReaction>()
            .HasOne(reaction => reaction.User)
            .WithMany(user => user.MunicipalityMessageReactions)
            .HasForeignKey(reaction => reaction.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MunicipalityUserBan>()
            .HasIndex(ban => new { ban.MunicipalityName, ban.UserId, ban.ExpiresAt });

        modelBuilder.Entity<MunicipalityUserBan>()
            .HasOne(ban => ban.User)
            .WithMany(user => user.MunicipalityUserBans)
            .HasForeignKey(ban => ban.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MunicipalityUserBan>()
            .HasOne(ban => ban.BannedByUser)
            .WithMany(user => user.IssuedMunicipalityUserBans)
            .HasForeignKey(ban => ban.BannedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
