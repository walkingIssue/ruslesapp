using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;

using rustles_platform.Client.Pages.Municipality.Services.Abstractions;
using rustles_platform.Client.Pages.Municipality.Services.Api;
using rustles_platform.Client.Pages.Municipality.Services.ChatSession;
using rustles_platform.Client.Pages.Municipality.Services.Geolocation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddScoped<HttpClient>(p => new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress, UriKind.Absolute) });
builder.Services.AddScoped<HubConnectionBuilder>(p => new HubConnectionBuilder());
builder.Services.AddScoped<IGeolocation, Geolocation>();
builder.Services.AddScoped<IRustlesGeoApi, RustlesGeoApi>();
builder.Services.AddTransient<IChatSession, MunipSession>();
builder.Services.AddTransient<IChatOrchestrator, ChatOrchestrator>();


await builder.Build().RunAsync();
