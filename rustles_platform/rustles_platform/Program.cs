using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Host.Mef;

using rustles.core.Registrations;
using rustles.core.Systems.GDAL.Abstractions;
using rustles.Infrastructure;
using rustles.Infrastructure.Model;
using rustles.Infrastructure.Registration;

using rustles_platform.Components;
using rustles_platform.Components.Account;

using SQLitePCL;

using System.CodeDom;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRustlesPositionResolutionPipeline();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddHostedService<MunipDispatcher>();

builder.Services.AddSingleton<UserHubManager>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddUserDataSqlite(connectionString);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<User>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<UsersDataContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<User>, IdentityNoOpEmailSender>();

builder.Services.AddSignalR();

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(rustles_platform.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapRasterEndPoint();

app.MapHub<MunicipalityHub>("/hubs/municipality");

app.Run();



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
    }
}

public interface IEndpoint
{
    static abstract void MapEndpoint(IEndpointRouteBuilder builder);
}
 [Flags] public enum HTTPEXTENSIOS { OK = 0, FAIL=1, HIGHPRESSURE=2, RETRY=4, BANNED = 8} 
public class RasterEnque : IEndpoint
{

    public static void MapEndpoint(IEndpointRouteBuilder builder) =>
        builder.MapPost("api/ConnectMunip", Handler);


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
            userLiveEntry = new(null, userRequestId, null, user!.PermanentUsername);
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
            recommeded = new TimeOnly(0, 0, 30)
        });
    }
}

