using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using BedBrigade.SpeakIt;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Serilog;
using System.Net;
using System.Security.Claims;

namespace BedBrigade.Client.Components.Pages
{
    public partial class Login : ComponentBase
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IAuthDataService _authDataService { get; set; }
        [Inject] private IAuthService _authService { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private IUserDataService _userDataService { get; set; }
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

        private EditContext? EC { get; set; }
        private ValidationMessageStore _validationMessageStore;
        protected bool _isBusy = false;
        protected string ForgotPasswordHref =>
            string.IsNullOrWhiteSpace(loginModel?.Email)
                ? "/forgot-password"
                : $"/forgot-password/{EncryptionLogic.EncodeUrl(loginModel.Email)}";


        protected override void OnInitialized()
        {
            _lc.InitLocalizedComponent(this);
            loginModel.Email = User;
            loginModel.Password = Password;
            passwordType = "password";
            showPassword = false;

            EC = new EditContext(loginModel);
            _validationMessageStore = new ValidationMessageStore(EC);
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
            if (!IsValid())
                return;

            try
            {
                _isBusy = true;
                ServiceResponse<ClaimsPrincipal> loginResult = await _authDataService.Login(loginModel.Email, loginModel.Password);

                if (loginResult.Success && loginResult.Data != null)
                {
                    await _authService.Login(loginResult.Data);

                    var userResult = await _userDataService.GetByEmail(loginModel.Email);
                    if (userResult.Success 
                        && userResult.Data != null 
                        && userResult.Data.MustChangePassword)
                    {
                        string oneTimePassword = EncryptionLogic.GetOneTimePassword(loginModel.Email);
                        string encodedEmail = EncryptionLogic.GetEncryptedEncodedEmail(loginModel.Email);
                        NavigationManager.NavigateTo($"/change-password/{oneTimePassword}/{encodedEmail}");
                    }
                    else if (!string.IsNullOrEmpty(returnUrl))
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
            finally
            {
                _isBusy = false;
            }
        }

        private bool IsValid()
        {
            _validationMessageStore.Clear();
            return ValidationLocalization.ValidateModel(loginModel, _validationMessageStore, _lc);
        }
    }
}
