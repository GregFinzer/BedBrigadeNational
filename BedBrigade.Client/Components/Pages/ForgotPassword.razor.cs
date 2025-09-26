using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BedBrigade.SpeakIt;
using ValidationLocalization = BedBrigade.SpeakIt.ValidationLocalization;

namespace BedBrigade.Client.Components.Pages;

public partial class ForgotPassword : ComponentBase
{
    [Parameter] public string? email { get; set; }

    [Inject] public IEmailBuilderService EmailBuilderService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject]
    public EmailQueueBackgroundService EmailQueueBackgroundService { get; set; } = default!;
    [Inject] private ILanguageContainerService _lc { get; set; }

    protected ForgotPasswordModel _model = new();
    protected bool _isBusy = false;
    protected bool _success = false;
    protected string? _errorMessage;
    private EditContext? EC { get; set; }
    private ValidationMessageStore _validationMessageStore;

    protected override void OnInitialized()
    {
        _lc.InitLocalizedComponent(this);
        EC = new EditContext(_model);
        _validationMessageStore = new ValidationMessageStore(EC);
    }

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            // Accept route-provided email and pre-populate the textbox
            _model.Email = EncryptionLogic.DecodeUrl(email!);
        }
    }

    private bool IsValid()
    {
        _validationMessageStore.Clear();
        return ValidationLocalization.ValidateModel(_model, _validationMessageStore, _lc);
    }

    protected async Task OnSubmitAsync()
    {
        if (!IsValid())
            return;

        _isBusy = true;
        _success = false;
        _errorMessage = null;

        try
        {
            var baseUrl = Nav.BaseUri; // use site base for email links
            ServiceResponse<bool> resp = await EmailBuilderService.SendForgotPasswordEmail(_model.Email!, baseUrl);

            if (resp.Success)
            {
                _success = true; // UI shows "Email sent"
                EmailQueueBackgroundService.SendNow();
            }
            else
            {
                _errorMessage = string.IsNullOrWhiteSpace(resp.Message)
                    ? _lc.Keys["UnableToSendEmail"]
                    : resp.Message;
            }
        }
        catch
        {
            _errorMessage = _lc.Keys["UnableToSendEmail"];
        }
        finally
        {
            _isBusy = false;
        }
    }


}

