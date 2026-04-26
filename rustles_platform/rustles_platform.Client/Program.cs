using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddScoped<HttpClient>(p => new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress, UriKind.Absolute) });

await builder.Build().RunAsync();
