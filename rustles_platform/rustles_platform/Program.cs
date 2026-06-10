using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.Host.Mef;

using rustles.core.Registrations;
using rustles.core.Systems.GDAL.Abstractions;
using rustles.Infrastructure;
using rustles.Infrastructure.Model;
using rustles.Infrastructure.Registration;

using rustles_platform.Components;
using rustles_platform.Components.Account;
using rustles_platform.Endpoints;
using rustles_platform.Hubs;
using rustles_platform.RateLimiting;
using rustles_platform.Services;

using SQLitePCL;

using System.CodeDom;
using System.Security.AccessControl;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddRustlesPositionResolutionPipeline();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddHostedService<MunipDispatcher>();

builder.Services.AddSingleton<UserHubManager>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IMunicipalityChatStore, MunicipalityChatStore>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();


builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddRustlesRateLimiting(isDevelopment);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddUserDataSqlite(connectionString);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<User>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<UsersDataContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<User>, IdentityNoOpEmailSender>();

builder.Services.AddSignalR();

builder.WebHost.UseStaticWebAssets();
var app = builder.Build();

await app.SeedTrustedTester();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(rustles_platform.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapRasterEndPoint();
app.MapMunicipalityModerationEndpoints();

app.MapHub<MunicipalityHub>("/hubs/municipality");

app.Run();



public class TrustedTesterManager
{
    readonly IServiceProvider _provider;
    public TrustedTesterManager(IServiceProvider provider)
    {
        _provider = provider;
    }
    public async Task SeedTesterAccount()
    {
        using var scope = _provider.CreateAsyncScope();

        var roleManger = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var exists = await roleManger.FindByNameAsync("TrustedTester");
        if(exists is null)
            await roleManger.CreateAsync(new IdentityRole("TrustedTester"));
    }
}

public static class TrustedTesterSeed
{
    public static async Task SeedTrustedTester(this WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var testers = new TrustedTesterManager(scope.ServiceProvider);

        await testers.SeedTesterAccount();
    }
}



public static class RustlesEndpoints
{
    public static IEndpointRouteBuilder MapRustlesEndpoints<TEndpoint>(this IEndpointRouteBuilder builder) where TEndpoint : IEndpoint
    {
        TEndpoint.MapEndpoint(builder);
        return builder;
    }

    public static void MapRasterEndPoint(this IEndpointRouteBuilder builder)
    {
        RasterEnque.MapEndpoint(builder); //RasterQue registration
        TrustedTester.MapEndpoint(builder);
    }
}

public interface IEndpoint
{
    static abstract void MapEndpoint(IEndpointRouteBuilder builder);
}
 [Flags] public enum HTTPEXTENSIOS { OK = 0, FAIL=1, HIGHPRESSURE=2, RETRY=4, BANNED = 8}


[Authorize(Roles = "TrustedTester")]
public class TrustedTester : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder builder)
    => builder.MapPost("/api/fbb0a13f-2a4a-477c-84a5-e5f18232b00e", Handler)
        .RequireAuthorization(p =>
        {
            p.RequireRole(["TrustedTester"]);
        })
        .WithRequestTimeout(TimeSpan.FromSeconds(30));

    public record MunipId(string MunipName);

    [Authorize(Roles = "TrustedTester")]
    private static async Task<IResult> Handler(
        [FromBody] MunipId munipId,
        HttpContext context,
        [FromServices] UserHubManager hubManger,
        [FromServices] IHubContext<MunicipalityHub, rustles.contracts.IMunipClientMethods> hubContext,
        [FromServices] IMunicipalityChatStore chatStore,
        [FromServices] UserManager<User> userManager,
        CancellationToken token)
    {
        var uid = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        User? user;
        if (uid is null || !Guid.TryParse(uid, out var uGuid) || (user = await userManager.FindByIdAsync(uid)) is null || !context.User.IsInRole("TrustedTester"))
            return TypedResults.NotFound();


        if (!hubManger.CurrentUserLedger.TryGetValue(uid, out var ledger)
            || ledger is null
            || ledger.SignalRConnectionId is null)
        {
            return TypedResults.BadRequest();
        }
        if (ledger.MunipId is not null)
            await hubContext.Groups.RemoveFromGroupAsync(ledger.SignalRConnectionId!, ledger.MunipId!, token);

        await hubContext.Groups.AddToGroupAsync(ledger.SignalRConnectionId!, munipId.MunipName, token);
        var catchup = await chatStore.GetCatchupAsync(munipId.MunipName, token);
        var pinnedMessages = await chatStore.GetPinnedMessagesAsync(munipId.MunipName, token);
        await hubContext.Clients.Client(ledger.SignalRConnectionId!).MunipConnect(
            new rustles.contracts.ConnectMunipAck(
                munipId.MunipName,
                UserDisplayName.Resolve(user),
                "Swapped With Call",
                catchup,
                pinnedMessages));
        lock (ledger.LockObject)
        {
            ledger.MunipId = munipId.MunipName;
        }

        return TypedResults.Ok();
    }
}

[Authorize]
public class RasterEnque : IEndpoint
{

    public static void MapEndpoint(IEndpointRouteBuilder builder) =>
        builder.MapPost("api/ConnectMunip", Handler)
        .WithRequestTimeout(TimeSpan.FromSeconds(30))
        .RequireAuthorization();


    public record MunipConnectionRequest(double longi, double latti);


    [Authorize]
    private static async Task<IResult> Handler(
        HttpContext context,
        [FromBody] MunipConnectionRequest request,
        [FromServices] IRequestPostion _requestPosition,
        [FromServices] UserHubManager hubManager,
        [FromServices] UserManager<rustles.Infrastructure.Model.User> userManager)
    {
        var uid = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (uid is null || !Guid.TryParse(uid, out var UGuid))
            return TypedResults.Unauthorized();

        UserHubManager.UserConnectionLedgerEntry userLiveEntry;
        rustles.Infrastructure.Model.User? user;
        var userRequestId = Guid.NewGuid();
        bool created = false;

        if (!hubManager.CurrentUserLedger.TryGetValue(uid, out var userCurrentRequest) || userCurrentRequest is null)
        {
            user = await userManager.FindByIdAsync(uid);
            if (user == null) return TypedResults.Unauthorized();
            userLiveEntry = new(null, userRequestId, null, UserDisplayName.Resolve(user!));
            created = true;
        }
        else userLiveEntry = userCurrentRequest;

        if (!created)
        {
            lock (userLiveEntry.LockObject)
            {
                userLiveEntry.RequestId = userRequestId;
            }

            if (!hubManager.CurrentUserLedger.TryUpdate(uid, userLiveEntry, userCurrentRequest))
                return HeavyLoadFail();
        }
        else if (!hubManager.CurrentUserLedger.TryAdd(uid, userLiveEntry)) 
            return HeavyLoadFail();

        
        var enqueResult = _requestPosition.QueResultionRequest(userRequestId, 
            new rustles.core.Entities.ClientPosition(request.longi, request.latti, UGuid));

        if (enqueResult.HasFlag(rustles.core.Shared.SystemAction.Error)
            || enqueResult.HasFlag(rustles.core.Shared.SystemAction.Failed))
            return TypedResults.UnprocessableEntity();
        
        return TypedResults.Ok();
    }

    private static IResult HeavyLoadFail()
    {
        return TypedResults.InternalServerError(new
        {
            extension = HTTPEXTENSIOS.FAIL | HTTPEXTENSIOS.HIGHPRESSURE | HTTPEXTENSIOS.RETRY,
            recommeded = new TimeSpan(0, 0, 30)
        });
    }
}

