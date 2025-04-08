using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Syncfusion.Blazor.DropDowns;

namespace BedBrigade.Client.Components.Pages.Administration.Admin
{
    public partial class Email : ComponentBase
    {
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private IUserDataService _svcUserDataService { get; set; }
        [Inject] private ILocationDataService _svcLocationDataService { get; set; }
        [Inject] private IScheduleDataService _svcScheduleDataService { get; set; }
        [Inject] private IEmailQueueDataService _svcEmailQueueDataService { get; set; }
        [Inject] private INewsletterDataService _svcNewsletterDataService { get; set; }

        public BulkEmailModel Model { get; set; } = new();
        private bool isSuccess;
        private bool isFailure;
        private bool showPlan;
        private string message;
        private bool isNationalAdmin;

        protected override async Task OnInitializedAsync()
        {
            Model.Locations = (await _svcLocationDataService.GetAllAsync()).Data;
            var user = (await _svcUserDataService.GetCurrentLoggedInUser()).Data;
            Model.Body = (await _svcUserDataService.GetEmailSignature(user.UserName)).Data;
            Model.Schedules = (await _svcScheduleDataService.GetFutureSchedulesByLocationId(user.LocationId)).Data;
            Model.CurrentLocationId = user.LocationId;
            isNationalAdmin = user.LocationId == Defaults.NationalLocationId;

            if (isNationalAdmin)
            {
                Model.EmailRecipientOptions = EnumHelper.GetEnumNameValues<EmailRecipientOption>();
            }
            else
            {
                Model.EmailRecipientOptions = EnumHelper.GetEnumNameValues<EmailRecipientOption>().Where(x => x.Value != EmailRecipientOption.Everyone).ToList();
            }
            
            Model.CurrentEmailRecipientOption = EmailRecipientOption.Myself;
            Model.ShowLocationDropdown = false;
            Model.ShowEventDropdown = false;
            Model.ShowNewsletterDropdown = false;
            await BuildPlan();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            //Collapse the mobile menu
            await _js.InvokeVoidAsync("AddRemoveClass.RemoveClass", "navbarResponsive", "show");
        }

        private async Task HandleValidSubmit()
        {
            EmailsToSendParms parms = new()
            {
                LocationId = Model.CurrentLocationId,
                Option = Model.CurrentEmailRecipientOption,
                ScheduleId = Model.CurrentScheduleId,
                NewsletterId = Model.CurrentNewsletterId
            };
            var emails = await _svcEmailQueueDataService.GetEmailsToSend(parms);
            var result = await _svcEmailQueueDataService.QueueBulkEmail(emails.Data, Model.Subject, Model.Body);
            if (result.Success)
            {
                ShowSuccess("Email successfully queued.");
            }
            else
            {
                ShowFailure("Email failed to queue. " + result.Message);
            }
        }

        private async void LocationChangeEvent(ChangeEventArgs<int, Location> args)
        {
            Model.CurrentLocationId = args.Value;
            Model.Schedules = (await _svcScheduleDataService.GetFutureSchedulesByLocationId(Model.CurrentLocationId)).Data;
            Model.Newsletters = (await _svcNewsletterDataService.GetAllForLocationAsync(Model.CurrentLocationId)).Data;
            Model.CurrentScheduleId = 0;
            Model.CurrentNewsletterId = 0;
            await BuildPlan();
            StateHasChanged();
        }

        private async Task BuildPlan()
        {
            EmailsToSendParms parms = new()
            {
                LocationId = Model.CurrentLocationId,
                Option = Model.CurrentEmailRecipientOption,
                ScheduleId = Model.CurrentScheduleId,
                NewsletterId = Model.CurrentNewsletterId
            };
            message = (await _svcEmailQueueDataService.GetSendPlanMessage(parms)).Data;
            showPlan = true;
        }

        private async void ScheduleChangeEvent(ChangeEventArgs<int, Common.Models.Schedule> args)
        {
            Model.CurrentNewsletterId = args.Value;
            await BuildPlan();
            StateHasChanged();
        }

        private async void NewsletterChangeEvent(ChangeEventArgs<int, Common.Models.Newsletter> args)
        {
            Model.CurrentScheduleId = args.Value;
            await BuildPlan();
            StateHasChanged();
        }

        private async void EmailRecipientChangeEvent(ChangeEventArgs<EmailRecipientOption, EnumNameValue<EmailRecipientOption>> args)
        {
            Model.CurrentEmailRecipientOption = args.Value;
            Model.ShowEventDropdown = Model.CurrentEmailRecipientOption.ToString().Contains("Event");
            Model.ShowLocationDropdown = isNationalAdmin && (Model.CurrentEmailRecipientOption.ToString().Contains("Location")
                || Model.CurrentEmailRecipientOption.ToString().Contains("Event"));
            Model.ShowNewsletterDropdown = Model.CurrentEmailRecipientOption.ToString().Contains("Newsletter");
            await BuildPlan();
            StateHasChanged();
        }

        public void ShowSuccess(string successMessage)
        {
            isSuccess = true;
            isFailure = false;
            showPlan = false;
            message = successMessage;
        }

        public void ShowFailure(string failureMessage)
        {
            isFailure = true;
            isSuccess = false;
            showPlan = false;
            message = failureMessage;
        }
    }
}
