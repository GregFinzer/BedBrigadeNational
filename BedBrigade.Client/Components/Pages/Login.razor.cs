using BedBrigade.Data.Models;
using BedBrigade.Client.Services;
using BedBrigade.Common;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;

namespace BedBrigade.Client.Components.Pages
{
    public partial class LoginBase : ComponentBase
    {
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IAuthService AuthService { get; set; }
        [Inject] private ICustomSessionService _sessionService { get; set; }
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Parameter] public string? User { get; set; }
        [Parameter] public string? Password { get; set; }

        protected UserLogin loginModel = new();
        protected string DisplayError { get; set; } = "none;";
        protected string Error = "";
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
            Console.WriteLine("In Login Razor Initialize");
            passwordType = "password";

            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("returnUrl", out var url))
            {
                returnUrl = url;
            }

        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            //Collapse the mobile menu
            await _js.InvokeVoidAsync("AddRemoveClass.RemoveClass", "navbarResponsive", "show");
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
            var result = await AuthService.Login(loginModel);
            if (result.Success)
            {

                await _sessionService.SetItemAsStringAsync(Constants.AuthToken, result.Data);
                await AuthenticationStateProvider.GetAuthenticationStateAsync();
                NavigationManager.NavigateTo("/Administration/Dashboard");
            }
            else
            {
                try
                {
                    errorMessage = result.Message;
                    DisplayError = "block;";
                    loginModel.Email = string.Empty;
                    loginModel.Password = string.Empty;
                    if (inputTextFocus.Element != null)
                    {
                        await inputTextFocus.Element.Value.FocusAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
