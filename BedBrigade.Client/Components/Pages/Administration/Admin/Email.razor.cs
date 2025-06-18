using System.Text;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.DropDowns;

namespace BedBrigade.Client.Components.Pages.Administration.Admin
{
    public partial class Email : ComponentBase
    {
        [Inject] private IUserDataService _svcUserDataService { get; set; }
        [Inject] private ILocationDataService _svcLocationDataService { get; set; }
        [Inject] private IScheduleDataService _svcScheduleDataService { get; set; }
        [Inject] private IEmailQueueDataService _svcEmailQueueDataService { get; set; }
        [Inject] private INewsletterDataService _svcNewsletterDataService { get; set; }
        [Inject] private IContentDataService _svcContentDataService { get; set; }
        [Inject] private IMailMergeLogic _mailMergeLogic { get; set; }
        [Inject] private NavigationManager _navigationManager { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        public BulkEmailModel Model { get; set; } = new();
        private bool isSuccess;
        private bool isFailure;
        private bool showPlan;
        private string message;
        private bool isNationalAdmin;
        private const string ErrorTitle = "Error";
       
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Log.Information($"{_svcAuth.UserName} went to the Bulk Email Page");

                await LoadLocations();
                await GetEmailSignature();
                await LoadSchedules();
                await LoadNewsletters();
                Model.EmailRecipientOptions = EnumHelper.GetEnumNameValues<EmailRecipientOption>().Where(x => x.Value != EmailRecipientOption.Everyone).ToList();
                Model.CurrentLocationId = _svcAuth.LocationId;

                isNationalAdmin = _svcAuth.IsNationalAdmin;

                if (_svcAuth.IsNationalAdmin)
                {
                    Model.EmailRecipientOptions = EnumHelper.GetEnumNameValues<EmailRecipientOption>();
                }

                Model.CurrentEmailRecipientOption = EmailRecipientOption.Myself;
                Model.ShowLocationDropdown = false;
                Model.ShowEventDropdown = false;
                Model.ShowNewsletterDropdown = false;
                await BuildPlan();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing Bulk Email component");
                _toastService.Error(ErrorTitle,"An error occurred while initializing the Email component: " + ex.Message);
            }
        }

        private async Task LoadNewsletters()
        {
            var newslettersResponse = await _svcNewsletterDataService.GetAllForLocationAsync(_svcAuth.LocationId);
            if (!newslettersResponse.Success || newslettersResponse.Data == null)
            {
                Log.Error("Bulk Email Failed to load newsletters: " + newslettersResponse.Message);
                _toastService.Error(ErrorTitle, "Failed to load newsletters: " + newslettersResponse.Message);
                return;
            }
            Model.Newsletters = newslettersResponse.Data;
        }

        private async Task LoadSchedules()
        {
            var scheduleResponse = await _svcScheduleDataService.GetFutureSchedulesByLocationId(_svcAuth.LocationId);

            if (!scheduleResponse.Success || scheduleResponse.Data == null)
            {
                Log.Error("Bulk Email Failed to load schedules: " + scheduleResponse.Message);
                _toastService.Error(ErrorTitle, "Failed to load schedules: " + scheduleResponse.Message);
                return;
            }

            Model.Schedules = scheduleResponse.Data;
        }

        private async Task GetEmailSignature()
        {
            var signatureResult = await _svcUserDataService.GetEmailSignature(_svcAuth.UserName);

            if (!signatureResult.Success)
            {
                Log.Error("Bulk Email Failed to load email signature: " + signatureResult.Message);
                _toastService.Error(ErrorTitle, "Failed to load email signature: " + signatureResult.Message);
                return;
            }

            Model.Body = signatureResult.Data;
        }

        private async Task LoadLocations()
        {
            var locationsResult = await _svcLocationDataService.GetAllAsync();
            if (!locationsResult.Success && locationsResult.Data != null)
            {
                Log.Error("Bulk Email Failed to load locations: " + locationsResult.Message);
                _toastService.Error(ErrorTitle, "Failed to load locations: " + locationsResult.Message);
                return;
            }
            Model.Locations = locationsResult.Data;
        }

        private async Task HandleValidSubmit()
        {
            try
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
                    Log.Information($"{_svcAuth.UserName} queued some Bulk Emails");
                    ShowSuccess("Email successfully queued.");
                }
                else
                {
                    ShowFailure("Email failed to queue. " + result.Message);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Bulk Email, Email failed to queue");
                ShowFailure("Email failed to queue. " + ex.Message);
            }
        }

        private async void LocationChangeEvent(ChangeEventArgs<int, Location> args)
        {
            try
            {
                Model.CurrentLocationId = args.Value;
                var scheduleResult =
                    await _svcScheduleDataService.GetFutureSchedulesByLocationId(Model.CurrentLocationId);

                if (!scheduleResult.Success || scheduleResult.Data == null)
                {
                    Log.Error("Bulk Email Failed to load schedules for the selected location: " + scheduleResult.Message);
                    _toastService.Error(ErrorTitle, "Failed to load schedules for the selected location: " + scheduleResult.Message);
                    return;
                }

                Model.Schedules = scheduleResult.Data;

                var newslettersResult = await _svcNewsletterDataService.GetAllForLocationAsync(Model.CurrentLocationId);

                if (!newslettersResult.Success || newslettersResult.Data == null)
                {
                    Log.Error("Bulk Email Failed to load newsletters for the selected location: " + newslettersResult.Message);
                    _toastService.Error(ErrorTitle, "Failed to load newsletters for the selected location: " + newslettersResult.Message);
                    return;
                }

                Model.Newsletters = newslettersResult.Data;
                Model.CurrentScheduleId = 0;
                Model.CurrentNewsletterId = 0;
                await BuildPlan();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Bulk Email LocationChangeEvent");
                ShowFailure("Bulk Email Location Change. " + ex.Message);
            }
        }

        private async Task BuildPlan()
        {
            try
            {
                EmailsToSendParms parms = new()
                {
                    LocationId = Model.CurrentLocationId,
                    Option = Model.CurrentEmailRecipientOption,
                    ScheduleId = Model.CurrentScheduleId,
                    NewsletterId = Model.CurrentNewsletterId
                };

                var planMessageResult = await _svcEmailQueueDataService.GetSendPlanMessage(parms);

                if (!planMessageResult.Success || planMessageResult.Data == null)
                {
                    Log.Error("Bulk Email, Failed to build email plan: " + planMessageResult.Message);
                    ShowFailure("Failed to build email plan: " + planMessageResult.Message);
                    return;
                }

                message = planMessageResult.Data;
                showPlan = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Bulk Email, Error building email plan");
                ShowFailure("Error building email plan: " + ex.Message);
            }
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
            try
            {
                var contentResult =
                    await _svcContentDataService.GetSingleByLocationAndContentType(Model.CurrentLocationId,
                        ContentType.NewsletterForm);

                if (!contentResult.Success || contentResult.Data == null)
                {
                    Log.Error("Bulk Email, Failed to get newsletter content: " + contentResult.Message);
                    ShowFailure("Failed to get newsletter content: " + contentResult.Message);
                    Model.Body = string.Empty;
                    return;
                }

                var locationResult = await _svcLocationDataService.GetByIdAsync(Model.CurrentLocationId);

                if (!locationResult.Success || locationResult.Data == null)
                {
                    Log.Error("Bulk Email, Failed to get location data: " + locationResult.Message);
                    ShowFailure("Failed to get location data: " + locationResult.Message);
                    Model.Body = string.Empty;
                    return;
                }

                var newsletterResult = await _svcNewsletterDataService.GetByIdAsync(Model.CurrentNewsletterId);

                if (!newsletterResult.Success || newsletterResult.Data == null)
                {
                    Log.Error("Bulk Email, Failed to get newsletter data: " + newsletterResult.Message);
                    ShowFailure("Failed to get newsletter data: " + newsletterResult.Message);
                    Model.Body = string.Empty;
                    return;
                }

                StringBuilder sb = new StringBuilder(contentResult.Data.ContentHtml);
                sb = _mailMergeLogic.ReplaceLocationFields(locationResult.Data, sb);
                sb = _mailMergeLogic.ReplaceBaseUrl(sb, _navigationManager.BaseUri);
                sb = _mailMergeLogic.ReplaceNewsletterNameForQuery(sb, newsletterResult.Data.Name);
                Model.Body = sb.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Bulk Email, Error building newsletter body");
                ShowFailure("Error building newsletter body: " + ex.Message);
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
