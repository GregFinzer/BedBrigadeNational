using BedBrigade.Common;
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.DropDowns;

namespace BedBrigade.Client.Pages.Administration.Admin
{
    public partial class Email : ComponentBase
    {
        [Inject] private IUserDataService _svcUserDataService { get; set; }
        [Inject] private ILocationDataService _svcLocationDataService { get; set; }
        [Inject] private IScheduleDataService _svcScheduleDataService { get; set; }
        [Inject] private IEmailQueueDataService _svcEmailQueueDataService { get; set; }
        
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
            isNationalAdmin = user.LocationId == Constants.NationalLocationId;

            if (isNationalAdmin)
            {
                Model.EmailRecipientOptions = EnumHelper.GetEnumNameValues<EmailRecipientOption>();
            }
            else
            {
                Model.EmailRecipientOptions = EnumHelper.GetEnumNameValues<EmailRecipientOption>().Where(x => x.Value != EmailRecipientOption.Everyone).ToList();
            }
            
            Model.CurrentEmailRecipientOption = EmailRecipientOption.BedBrigadeLeadersForLocation;
            Model.ShowLocationDropdown = isNationalAdmin;
            Model.ShowEventDropdown = false;
            await BuildPlan();
        }
        
        private async Task HandleValidSubmit()
        {
            var emails = await _svcEmailQueueDataService.GetEmailsToSend(Model.CurrentLocationId, Model.CurrentEmailRecipientOption, Model.CurrentScheduleId);
            var result = await EmailQueueLogic.QueueBulkEmail(emails.Data, Model.Subject, Model.Body);
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
            Model.CurrentScheduleId = 0;
            await BuildPlan();
            StateHasChanged();
        }

        private async Task BuildPlan()
        {
            message = (await _svcEmailQueueDataService.GetSendPlanMessage(Model.CurrentLocationId, Model.CurrentEmailRecipientOption, Model.CurrentScheduleId)).Data;
            showPlan = true;
        }

        private async void ScheduleChangeEvent(ChangeEventArgs<int, Data.Models.Schedule> args)
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
