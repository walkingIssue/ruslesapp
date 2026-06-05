using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using rustles.Infrastructure;
using rustles.Infrastructure.Model;
using rustles_platform.Services;

namespace rustles_platform.tests.Services;

internal sealed class ChatStoreFixture : IAsyncDisposable
{
    private const string InMemoryConnectionString = "DataSource=:memory:";

    private readonly SqliteConnection _connection;
    private readonly UsersDataContext _dbContext;
    private readonly ServiceProvider? _serviceProvider;

    private ChatStoreFixture(
        SqliteConnection connection,
        UsersDataContext dbContext,
        TestClock clock,
        ServiceProvider? serviceProvider = null)
    {
        _connection = connection;
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        Clock = clock;
        Store = new MunicipalityChatStore(dbContext, clock);
    }

    public string UserId { get; } = Guid.NewGuid().ToString();
    public string SecondUserId { get; } = Guid.NewGuid().ToString();
    public TestClock Clock { get; }
    public MunicipalityChatStore Store { get; }
    public UsersDataContext DbContext => _dbContext;

    public static async Task<ChatStoreFixture> CreateAsync(bool useMigrations = false)
    {
        var connection = new SqliteConnection(InMemoryConnectionString);
        await connection.OpenAsync();

        var contextScope = useMigrations
            ? await CreateMigratedContextAsync(connection)
            : await CreateEnsuredContextAsync(connection);

        var fixture = new ChatStoreFixture(
            connection,
            contextScope.Context,
            new TestClock(),
            contextScope.ServiceProvider);

        await fixture.SeedUsersAsync();

        return fixture;
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }

        await _connection.DisposeAsync();
    }

    private static async Task<ContextScope> CreateMigratedContextAsync(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddDbContext<UsersDataContext>(builder => builder.UseSqlite(connection));
        services
            .AddIdentityCore<User>(identityOptions =>
            {
                identityOptions.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<UsersDataContext>();

        var serviceProvider = services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<UsersDataContext>();
        await dbContext.Database.MigrateAsync();

        return new ContextScope(dbContext, serviceProvider);
    }

    private static async Task<ContextScope> CreateEnsuredContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<UsersDataContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new UsersDataContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        return new ContextScope(dbContext);
    }

    private async Task SeedUsersAsync()
    {
        _dbContext.Users.AddRange(
            new User { Id = UserId, UserName = "alice@example.com", PermanentUsername = "alice" },
            new User { Id = SecondUserId, UserName = "bob@example.com", PermanentUsername = "bob" });
        await _dbContext.SaveChangesAsync();
    }

    private sealed record ContextScope(UsersDataContext Context, ServiceProvider? ServiceProvider = null);
}
