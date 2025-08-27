using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using BedBrigade.SpeakIt;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BedBrigade.Client.Components.Pages;

public partial class ChangePassword : ComponentBase
{
    [Parameter] public string encryptedEmail { get; set; } = string.Empty;
    [Parameter] public string oneTimePassword { get; set; } = string.Empty;

    [Inject] public IUserDataService UserDataService { get; set; } = default!;
    [Inject] public IAuthDataService AuthDataService { get; set; } = default!;
    [Inject] public IAuthService AuthService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] private ILanguageContainerService _lc { get; set; }

    protected User? _user;
    protected bool _oneTimePasswordValid;
    protected bool _isLoading = true;
    protected bool _isBusy = false;
    protected bool _success = false;
    protected string? _errorMessage;
    protected bool _showPassword = false;

    protected ChangePasswordModel _model = new();
    public string email;
    private EditContext? EC { get; set; }
    private ValidationMessageStore _validationMessageStore;

    protected override void OnInitialized()
    {
        _lc.InitLocalizedComponent(this);
        EC = new EditContext(_model);
        _validationMessageStore = new ValidationMessageStore(EC);
    }

    protected override async Task OnParametersSetAsync()
    {
        string loadErrorMessage = _lc.Keys["ChangePasswordExpired"];
        try
        {
            // Lookup user
            if (!string.IsNullOrEmpty(encryptedEmail))
            {
                email = EncryptionLogic.DecryptEmail(encryptedEmail);
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
            _isLoading = false;
        }
    }

    protected async Task OnSubmitAsync()
    {
        if (_user is null || !_oneTimePasswordValid) return;

        if (!IsValid())
        {
            return;
        }

        _isBusy = true;
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
                _errorMessage = _lc.Keys["UnableToChangePassword"];
            }
        }
        catch
        {
            _errorMessage = _lc.Keys["UnableToChangePassword"];
        }
        finally
        {
            _isBusy = false;
        }
    }

    private bool IsValid()
    {
        _validationMessageStore.Clear();
        return ValidationLocalization.ValidateModel(_model, _validationMessageStore, _lc);
    }


}

