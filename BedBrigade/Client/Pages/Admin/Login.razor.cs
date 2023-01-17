using BedBrigade.Client.Services.AuthService;
using BedBrigade.Shared;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Notifications;
using Blazored.LocalStorage;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace BedBrigade.Pages.Admin
{
    public partial class LoginBase : ComponentBase
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        //[Inject] private IUserService _svcUser { get; set; }
        [Inject] private IAuthService AuthService { get; set; }
        [Inject] private ILocalStorageService _local  { get; set; }
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Parameter] public string? User { get; set; }
        [Parameter] public string? Password { get; set; }

        protected UserLogin loginModel = new();
        protected string DisplayError { get; set; } = "none;";
        protected string Error = "";
        protected StringValues returnUrl;

        protected SfToast? ToastObj;
        protected string? ToastContent;
        protected int ToastTimeout;
        protected string ToastTitle = string.Empty;
        protected string? errorMessage;
        protected InputText? inputTextFocus;

        public bool showPassword { get; set; }
        public string? passwordType { get; set; }
        public string Message { get; set; }

        protected override void OnInitialized()
        {
            loginModel.Email = User;
            loginModel.Password= Password;
            Console.WriteLine("In Login Razor Initialize");
            passwordType = "password";

            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("returnUrl", out var url))
            {
                returnUrl = url;
            }

        }

        protected async Task HandlePassword()
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

                await _local.SetItemAsync("authToken", result.Data);
                await AuthenticationStateProvider.GetAuthenticationStateAsync();

                NavigationManager.NavigateTo(returnUrl);
            }
            else
            {
                try
                {
                    
                    ToastTitle = "Login Error";
                    ToastContent = "The credentials entered are not valid.<br/> Please try again.";
                    ToastTimeout = 5000;

                    await ToastObj.ShowAsync();

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
;
            }
        }


        //protected async Task HandleLogin()
        //{
        //    var result = await _svcUser.AuthenicateAsync(loginModel);
        //    if (result.Successful)
        //    {
        //        await _local.SetItemAsync<string>("authToken", result.Token);
        //        var loggedin = await _svcUser.LoginAsync(result.Token);
        //        if (loggedin)
        //        {
        //            Identity = new(await _local.GetItemAsync<string>("authToken"));
        //            if (result.TwoFactor)
        //            {
        //                if (Identity.TwoFactorExpires.Date > DateTime.Now.Date)
        //                {
        //                    NavigationManager.NavigateTo("/dash", true);
        //                }
        //                else
        //                {
        //                    NavigationManager.NavigateTo("/TwoFactor", true);
        //                }
        //            }
        //            else
        //            {
        //                NavigationManager.NavigateTo("/dash", true);
        //            }
        //        }
        //        else
        //        {
        //            throw new Exception("Unable to login after authenication");
        //        }
        //    }
        //    else
        //    {
        //        try
        //        {
        //            ToastObj.Title = "Login Error";
        //            ToastObj.Content = "The credentials entered are not valid.<br/> Please try again.";
        //            ToastObj.Timeout = 5000;

        //            await ToastObj.Show();
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.ToString());
        //        }

        //        // DisplayError = "normal;";
        //    }
        //}
    }
}
