using BedBrigade.Client;
using BedBrigade.Client.Components;

var builder = WebApplication.CreateBuilder(args);

StartupLogic.ConfigureLogger(builder);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add services to the container
StartupLogic.AddServicesToTheContainer(builder);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
