using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
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
using System.Collections.Concurrent;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRustlesPositionResolutionPipeline();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

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

app.MapHub<MunicipalityHub>("/hubs/municipality");

app.Run();


public class MunipGroup
{
    private readonly Guid munipId = Guid.NewGuid();

    public int PublicMunipId { get; set; }
    public string GroupId {
        get {
            return $"{munipId}_JumpingBunny_{PublicMunipId}";
        }
    }
    public string Name { get; init; } = string.Empty;

    public MunipGroup(int id, string name)
    {
        PublicMunipId = id;
        Name = name;
    }

    public static IEnumerable<MunipGroup> InstantiateMunips(int count)
    {
        for(int i = count; i > 0; i--)
        {
            yield return new MunipGroup(i, $"Munip-{i}");
        }
    }
}


public class UserHubManager
{
    public List<MunipGroup> munipGroups = MunipGroup.InstantiateMunips(20).ToList();

    public ConcurrentDictionary<MunipGroup, ConcurrentQueue<string>> MunipChats { get; set; } = new();

    public ConcurrentDictionary<string, string> MunipByUser { get; set; } = new();
 
}

[Authorize]
public class MunicipalityHub : Microsoft.AspNetCore.SignalR.Hub
{
    public MunicipalityHub(UserHubManager manager)
    {
        _manager = manager;
    }
    
    private readonly UserHubManager _manager;
    
    [Flags]
    public enum ConnectionFault { ok= 0, fail = 1, DoesntExist = 2, OutofOrder =4 } 

    private ConnectionFault f_e()
    {
        return ConnectionFault.fail | ConnectionFault.DoesntExist;
    }
    private ConnectionFault f_O()
    {
        return ConnectionFault.fail | ConnectionFault.OutofOrder;
    }
    

    public async Task ConnectUser(int chatId)
    {

        if (chatId < 0 || chatId > _manager.munipGroups.Count)
        {
            await Clients.Caller.SendAsync("FailedToConnect", f_e());
            return;
        }

        var userMunip = UserMunip(chatId);

        if (userMunip is null)
        {
            await Clients.Caller.SendAsync("FailedToConnect", f_O());
            return;
        }

        var currentMessages = CurrentMessages(chatId);

        if (currentMessages == null)
            await Clients.Caller.SendAsync("ConnectionError", f_e());

        if (_manager.MunipByUser.TryGetValue(Context.ConnectionId, out var value))
            _manager.MunipByUser.TryUpdate(Context.ConnectionId, userMunip!.GroupId, value);

        else _manager.MunipByUser.TryAdd(Context.ConnectionId, userMunip!.GroupId);

        await Groups.AddToGroupAsync(Context.ConnectionId, userMunip!.GroupId);
        await Clients.Caller.SendAsync("MunipConnect", $"You have been connected to chat {chatId}", currentMessages);
        
    }

    public async Task MunipChat(string message)
    {
        if(_manager.MunipByUser.TryGetValue(Context.ConnectionId, out var munipId) && munipId is not null){
            var t = Clients.OthersInGroup(munipId).SendAsync("MessageByUser", Context.ConnectionId, message);
            var uid = new StringBuilder().Append(Context.ConnectionId.Take(4).ToArray()).Append("..").Append(Context.ConnectionId.Take(new Range(6, 13)).ToArray()).ToString();
            if (_manager.MunipChats.TryGetValue(UserMunip(munipId)!, out var que))
                que.Enqueue($"{uid}: {message}");
            await t;
        }
    }
    
    private MunipGroup? UserMunip(int chatId)
    {
        return _manager.munipGroups.Where(x => x.PublicMunipId == chatId).SingleOrDefault();
    }

    private MunipGroup? UserMunip(string groupId) => _manager.munipGroups.Where(x => x.GroupId == groupId).SingleOrDefault();

    private List<string>? CurrentMessages(int id)
    {
        var item = UserMunip(id);

        if (item is null) return null;

        if(_manager.MunipChats.TryGetValue(item, out var CurrentChat))
        {
            return CurrentChat.ToList();
        }

        string[] newChat = ["... can you hear the rustling through the breeze"];
        var concurrent = new ConcurrentQueue<string>(newChat);
        if(!_manager.MunipChats.TryAdd(item, concurrent))
        {
            //assume the we got descheduled and try again.
            _manager.MunipChats.TryAdd(item, concurrent);
        }
        return newChat.ToList();
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("ConnectionInit", "Welcome, you are connected.");
        await base.OnConnectedAsync();
    }
}
