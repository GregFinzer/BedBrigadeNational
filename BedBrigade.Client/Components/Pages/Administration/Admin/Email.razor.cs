using System.Text;
using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
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
        [Inject] private IContentDataService _svcContentDataService { get; set; }
        [Inject] private IMailMergeLogic _mailMergeLogic { get; set; }
        [Inject] private NavigationManager _navigationManager { get; set; }
        public BulkEmailModel Model { get; set; } = new();
        private bool isSuccess;
        private bool isFailure;
        private bool showPlan;
        private string message;
        private bool isNationalAdmin;

        protected override async Task OnInitializedAsync()
        {
            await LoadLocations();
            User? user = await GetCurrentUser();
            await GetEmailSignature(user);
            await LoadSchedules(user);
            await LoadNewsletters(user);
            Model.EmailRecipientOptions = EnumHelper.GetEnumNameValues<EmailRecipientOption>().Where(x => x.Value != EmailRecipientOption.Everyone).ToList();

            if (user != null)
            {
                Model.CurrentLocationId = user.LocationId;

                isNationalAdmin = user.LocationId == Defaults.NationalLocationId;

                if (isNationalAdmin)
                {
                    Model.EmailRecipientOptions = EnumHelper.GetEnumNameValues<EmailRecipientOption>();
                }
            }
            
            Model.CurrentEmailRecipientOption = EmailRecipientOption.Myself;
            Model.ShowLocationDropdown = false;
            Model.ShowEventDropdown = false;
            Model.ShowNewsletterDropdown = false;
            await BuildPlan();
        }

        private async Task LoadNewsletters(User? user)
        {
            if (user != null)
            {
                var newslettersResponse = await _svcNewsletterDataService.GetAllForLocationAsync(user.LocationId);
                if (!newslettersResponse.Success || newslettersResponse.Data == null)
                {
                    Log.Error(newslettersResponse.Message);
                    ShowFailure("Failed to load newsletters: " + newslettersResponse.Message);
                    return;
                }
                Model.Newsletters = newslettersResponse.Data;
            }
        }

        private async Task LoadSchedules(User? user)
        {
            if (user != null)
            {
                var scheduleResponse = await _svcScheduleDataService.GetFutureSchedulesByLocationId(user.LocationId);

                if (!scheduleResponse.Success || scheduleResponse.Data == null)
                {
                    Log.Error(scheduleResponse.Message);
                    ShowFailure("Failed to load schedules: " + scheduleResponse.Message);
                    return;
                }

                Model.Schedules = scheduleResponse.Data;
            }
        }

        private async Task GetEmailSignature(User? user)
        {
            if (user != null)
            {
                var signatureResult = await _svcUserDataService.GetEmailSignature(user.UserName);

                if (!signatureResult.Success)
                {
                    Log.Error(signatureResult.Message);
                    ShowFailure("Failed to load email signature: " + signatureResult.Message);
                    return;
                }

                Model.Body = signatureResult.Data;
            }
        }

        private async Task<User?> GetCurrentUser()
        {
            var userResult = await _svcUserDataService.GetCurrentLoggedInUser();
            
            if (!userResult.Success || userResult.Data == null)
            {
                Log.Error(userResult.Message);
                ShowFailure("Failed to load user data: " + userResult.Message);
                return null;
            }
            return userResult.Data;
        }

        private async Task LoadLocations()
        {
            var locationsResult = await _svcLocationDataService.GetAllAsync();
            if (!locationsResult.Success && locationsResult.Data != null)
            {
                Log.Error(locationsResult.Message);
                ShowFailure("Failed to load locations: " + locationsResult.Message);
                return;
            }
            Model.Locations = locationsResult.Data;
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
            Model.CurrentScheduleId = args.Value;
            await BuildPlan();
            StateHasChanged();
        }

        private async void NewsletterChangeEvent(ChangeEventArgs<int, Common.Models.Newsletter> args)
        {
            Model.CurrentNewsletterId = args.Value;
            await BuildNewsletterBody();
            await BuildPlan();
            StateHasChanged();
        }

        private async Task BuildNewsletterBody()
        {
            var contentResult =
                await _svcContentDataService.GetSingleByLocationAndContentType(Model.CurrentLocationId,
                    ContentType.NewsletterForm);

            var locationResult = await _svcLocationDataService.GetByIdAsync(Model.CurrentLocationId);

            var newsletterResult = await _svcNewsletterDataService.GetByIdAsync(Model.CurrentNewsletterId);

            if (contentResult.Success && locationResult.Success && newsletterResult.Success)
            {
                StringBuilder sb = new StringBuilder(contentResult.Data.ContentHtml);
                sb = _mailMergeLogic.ReplaceLocationFields(locationResult.Data, sb);
                sb = _mailMergeLogic.ReplaceBaseUrl(sb, _navigationManager.BaseUri);
                sb = _mailMergeLogic.ReplaceNewsletterNameForQuery(sb, newsletterResult.Data.Name);
                Model.Body = sb.ToString();
            }
            else
            {
                Model.Body = string.Empty;
            }
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
