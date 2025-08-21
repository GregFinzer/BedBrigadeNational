using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Components.Pages;

public partial class ForgotPassword : ComponentBase
{
    [Parameter] public string? email { get; set; }

    [Inject] public IEmailBuilderService EmailBuilderService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject]
    public EmailQueueBackgroundService EmailQueueBackgroundService { get; set; } = default!;

    protected ForgotPasswordModel _model = new();
    protected bool _busy = false;
    protected bool _success = false;
    protected string? _errorMessage;

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            // Accept route-provided email and pre-populate the textbox
            _model.Email = EncryptionLogic.FromBase64Url(email!);
        }
    }

    protected async Task OnSubmitAsync()
    {
        _busy = true;
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
                    ? "Unable to send email. Please try again."
                    : resp.Message;
            }
        }
        catch
        {
            _errorMessage = "Unable to send email. Please try again.";
        }
        finally
        {
            _busy = false;
        }
    }

    protected sealed class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string? Email { get; set; }
    }
}

