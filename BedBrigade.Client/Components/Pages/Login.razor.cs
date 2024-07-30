using BedBrigade.Data.Models;
using BedBrigade.Client.Services;
using BedBrigade.Common;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;

namespace BedBrigade.Client.Components.Pages
{
    public partial class Login : ComponentBase
    {
        [CascadingParameter]
        private Task<AuthenticationState>? _authenticationState { get; set; }
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IAuthDataService _authDataService { get; set; }
        [Inject] private IAuthService AuthService { get; set; }

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
            loginModel.Email = User;
            loginModel.Password = Password;
            passwordType = "password";
            showPassword = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
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
            if (firstRender && !AuthService.IsLoggedIn)
            {
                var restoredFromState = await AuthService.GetStateFromTokenAsync();
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
            var loginResult = await _authDataService.Login(loginModel.Email, loginModel.Password);

            if (!loginResult.Success)
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
            else
            {
                await AuthService.Login(loginResult.Data);

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    NavigationManager.NavigateTo(returnUrl);
                    returnUrl = string.Empty;
                }
                else
                {
                    NavigationManager.NavigateTo("/Administration/Dashboard");
                }
            }
        }
    }
}
