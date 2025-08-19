using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Components.Pages;

public partial class ChangePassword : ComponentBase
{
    [Parameter] public string email { get; set; } = string.Empty;
    [Parameter] public string oneTimePassword { get; set; } = string.Empty;

    [Inject] public IUserDataService UserDataService { get; set; } = default!;
    [Inject] public IAuthDataService AuthDataService { get; set; } = default!;
    [Inject] public IAuthService AuthService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    protected User? _user;
    protected bool _oneTimePasswordValid;
    protected bool _loading = true;
    protected bool _busy = false;
    protected bool _success = false;
    protected string? _errorMessage;
    protected bool _showPassword = false;

    protected ChangePasswordModel _model = new();

    protected override async Task OnParametersSetAsync()
    {
        const string loadErrorMessage = "Change password expired, request Forgot Password again.";
        try
        {
            // Lookup user
            if (AuthService.IsLoggedIn)
            {
                email = AuthService.Email;
                var userResp = await UserDataService.GetByEmail(AuthService.Email);
                _user = (userResp?.Success ?? false) ? userResp!.Data : null;
            }
            else if (!string.IsNullOrEmpty(email))
            {
                var userResp = await UserDataService.GetByEmail(email);
                _user = (userResp?.Success ?? false) ? userResp!.Data : null;
            }
            else
            {
                _errorMessage = loadErrorMessage;
                return;
            }



            // Validate One Time Password (GetOneTimePassword enforces 15-min window)
            var expected = EncryptionLogic.GetOneTimePassword(email);
            _oneTimePasswordValid =
                _user is not null && string.Equals(expected, oneTimePassword, StringComparison.Ordinal);

            if (!_oneTimePasswordValid)
            {
                _errorMessage = loadErrorMessage;
            }
        }
        catch
        {
            _errorMessage = loadErrorMessage;
            _oneTimePasswordValid = false;
        }
        finally
        {
            _loading = false;
        }
    }

    protected async Task OnSubmitAsync()
    {
        if (_user is null || !_oneTimePasswordValid) return;

        _busy = true;
        _errorMessage = null;

        try
        {
            // userName comes from the retrieved User.UserName
            var result =
                await AuthDataService.ChangePassword(_user.UserName, _model.Password!, mustChangePassword: false);

            if (result is not null && (result.Success && result.Data is not null))
            {
                _success = true;
                _oneTimePasswordValid = false; // hide the form after success
                await AuthService.LogoutAsync();
            }
            else
            {
                _errorMessage = "Unable to change password. Please try again.";
            }
        }
        catch
        {
            _errorMessage = "Unable to change password. Please try again.";
        }
        finally
        {
            _busy = false;
        }
    }

    protected sealed class ChangePasswordModel
    {
        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
            ErrorMessage = "Password must be 8+ chars with upper, lower, number, and special character")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare(nameof(Password), ErrorMessage = "Passwords must match")]
        public string? ConfirmPassword { get; set; }
    }
}

