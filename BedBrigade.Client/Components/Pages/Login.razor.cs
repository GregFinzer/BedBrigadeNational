using System.Security.Claims;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace BedBrigade.Client.Components.Pages
{
    public partial class Login : ComponentBase
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IAuthDataService _authDataService { get; set; }
        [Inject] private IAuthService _authService { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }

        [Parameter] public string? User { get; set; }
        [Parameter] public string? Password { get; set; }

        protected UserLogin loginModel = new();
        protected string DisplayError { get; set; } = "none;";
        protected StringValues returnUrl;

        protected string? errorMessage;
        protected InputText? inputTextFocus;

        public bool showPassword { get; set; }
        public string? passwordType { get; set; }
        public string Message { get; set; }

        protected override void OnInitialized()
        {
            _lc.InitLocalizedComponent(this);
            loginModel.Email = User;
            loginModel.Password = Password;
            passwordType = "password";
            showPassword = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                //Parse the query string for the return URL, so we can go there after login
                if (String.IsNullOrEmpty(returnUrl))
                {
                    var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
                    if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("returnUrl", out var url))
                    {
                        returnUrl = url;
                    }
                }

                //A hard reset was performed or the session was lost, try to restore the state and redirect back to the returnUrl
                if (firstRender && !_authService.IsLoggedIn)
                {
                    var restoredFromState = await _authService.GetStateFromTokenAsync();
                    if (restoredFromState)
                    {
                        if (!string.IsNullOrEmpty(returnUrl))
                        {
                            NavigationManager.NavigateTo(returnUrl);
                            returnUrl = string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Login.OnAfterRenderAsync");
                errorMessage = "There was an error loading the page, try again later.";
                DisplayError = "block;";
            }
        }


        protected void HandlePassword()
        {
            if (showPassword)
            {
                passwordType = "password";
                showPassword = false;
            }
            else
            {
                passwordType = "text";
                showPassword = true;
            }
        }

        protected async Task HandleLogin()
        {
            try
            {
                ServiceResponse<ClaimsPrincipal> loginResult = await _authDataService.Login(loginModel.Email, loginModel.Password);

                if (loginResult.Success && loginResult.Data != null)
                {
                    await _authService.Login(loginResult.Data);

                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        NavigationManager.NavigateTo(returnUrl);
                        returnUrl = string.Empty;
                    }
                    else
                    {
                        Log.Logger.Information($"User {_authService.UserName} logged in");
                        NavigationManager.NavigateTo("/Administration/Dashboard");
                    }
                }
                else
                {
                    errorMessage = loginResult.Message;
                    DisplayError = "block;";
                    loginModel.Email = string.Empty;
                    loginModel.Password = string.Empty;
                    if (inputTextFocus.Element != null)
                    {
                        await inputTextFocus.Element.Value.FocusAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Login.HandleLogin");
                errorMessage = "There was an error logging in, try again later.";
                DisplayError = "block;";
            }
        }
    }
}
