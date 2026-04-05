using Microsoft.EntityFrameworkCore;

using rustles.Infrastructure.Model;
using rustles.Infrastructure.Registration;

namespace rustles.Infrastructure;

public class UsersDataContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Message { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Consent> Consents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
