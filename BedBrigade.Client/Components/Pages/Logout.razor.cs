using BedBrigade.Client.Services;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace BedBrigade.Client.Components.Pages;

public partial class Logout : ComponentBase
{
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private IAuthService AuthService { get; set; }
    [Inject] private ILanguageContainerService _lc { get; set; }
    [Inject] private ILocationState _locationState { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    [Parameter] public string? reason { get; set; }
    

    protected override async Task OnInitializedAsync()
    {
        _lc.InitLocalizedComponent(this);

        try
        {
            if (AuthService.IsLoggedIn)
            {
                string locationRoute = AuthService.UserRoute.TrimStart('/');
                await AuthService.LogoutAsync();

                if (reason != "idle")
                {
                    NavigationManager.NavigateTo($"/{locationRoute}");
                }
            }
            else
            {
                await AuthService.LogoutAsync();
                if (reason == "idle")
                {
                    await _locationState.NotifyStateChangedAsync();
                }
                else
                {
                    NavigationManager.NavigateTo("/");
                }
            }
        }
        catch (System.InvalidOperationException)
        {
            // This can happen if JavaScript is being statically rendered.
            // We can safely ignore this error.
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, $"Logout.OnInitializedAsync");
            ErrorMessage = "There was an error loading the page, try again later.";
        }
    }


}

