using Microsoft.AspNetCore.Components;

using rustles.application.Shared.Services;
using rustles.application.Web.Components;
using rustles.application.Web.Realtime;
using rustles.application.Web.Services;
using rustles.core.Entities;
using rustles.core.Registrations;
using rustles.core.Shared;
using rustles.core.Systems.GDAL.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddScoped(sp =>
{
    var navigation = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigation.BaseUri) };
});
builder.Services.AddRustlesPositionResolutionPipeline();
builder.Services.AddSingleton<MunicipalityConnectionRegistry>();
builder.Services.AddSingleton<MunicipalityChatStore>();
builder.Services.AddHostedService<PositionResolutionDispatcher>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapPost("/api/resolve-position", (PositionResolveRequest request, IRequestPostion positionResolver) =>
{
    var requestId = Guid.NewGuid();
    var action = positionResolver.QueResultionRequest(
        requestId,
        new ClientPosition(request.Longitude, request.Latitude, request.UserId));

    if (action.HasFlag(SystemAction.Failed) || action.HasFlag(SystemAction.Unresolvable))
        return Results.Json(new PositionResolveRejected(action.ToString()), statusCode: StatusCodes.Status406NotAcceptable);

    return Results.Ok(new PositionResolveQueued(requestId, action.ToString()));
});

app.MapHub<MunicipalityChatHub>("/hubs/munip-chat");

app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(typeof(rustles.application.Shared._Imports).Assembly);

app.Run();
