using BedBrigade.Client;
using BedBrigade.Client.Services.AuthService;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSyncfusionBlazor();
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("OTY1MjQ3QDMyMzAyZTM0MmUzMEJwUmQxRzhxTzFVRFN1RmFHc1VQdGZicmhyOVhVcW5MKy9NZXlPbEtETms9");

await builder.Build().RunAsync();
