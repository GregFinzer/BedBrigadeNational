using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Inputs;

namespace BedBrigade.Client.Components.Pages.Administration.AdminTasks
{
    public partial class AddEditBedRequest : ComponentBase, IDisposable
    {
        [Parameter] public int LocationId { get; set; }
        [Parameter] public int? BedRequestId { get; set; }

        [Inject] private IBedRequestDataService? _svcBedRequest { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private IGeoLocationQueueDataService? _svcGeoLocation { get; set; }
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private IContentDataService? _svcContent { get; set; }
        [Inject] private ToastService? _toastService { get; set; }
        [Inject] private IJSRuntime? JS { get; set; }
        [Inject] private ILanguageContainerService? _lc { get; set; }
        [Inject] private NavigationManager? _nav { get; set; }
        [Inject] private IEmailBuilderService _svcEmailBuilder { get; set; }
        [Inject] private IScheduleDataService _svcSchedule { get; set; }
        [Inject] private ISendSmsLogic _sendSmsLogic { get; set; }
        [Inject] private IEmailQueueDataService _emailQueueDataService { get; set; } = null!;
        [Inject] private ISmsQueueDataService _smsQueueDataService { get; set; } = null!;

        [Parameter] public string? Id { get; set; }

        protected BedBrigade.Common.Models.BedRequest? Model { get; set; }

        protected List<Location>? Locations { get; set; }
        protected List<BedBrigade.Common.Models.Schedule> FutureDeliverySchedules { get; set; } = new();
        protected List<string>? lstPrimaryLanguage;
        protected List<string>? lstSpeakEnglish;
        protected List<BedRequestEnumItem>? BedRequestStatuses { get; private set; }
        private EditContext? _editContext;
        private ValidationMessageStore? _validationMessageStore;
        private List<UsState>? StateList = AddressHelper.GetStateList();
        public required SfMaskedTextBox zipTextBox;
        public required SfMaskedTextBox phoneTextBox;

        private const string IsError = "Error";
        private const string BRService = "_svcBedRequest service is not available.";

        private bool _showConfirmAddScheduleDialog;
        private string _confirmAddScheduleTitle = string.Empty;
        private string _confirmAddScheduleMessage = string.Empty;
        private TaskCompletionSource<bool>? _confirmAddScheduleTcs;
        private BedRequestStatus? _originalStatus;
        private BedRequestStatus? _lastSelectedStatus;

        protected bool OnlyRead { get; set; } = false;
        public string SpeakEnglishVisibility = "hidden";
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "3" },
        };

        protected bool IsNew => !BedRequestId.HasValue || BedRequestId == 0;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc?.InitLocalizedComponent(this);
                BedRequestStatuses = EnumHelper.GetBedRequestStatusItems();
                await LoadConfiguration();
                await LoadLocations();
                await LoadModel();
                InitializeValidationContext();
                await LoadDeliverySchedules(Model?.LocationId ?? LocationId);

                // Ensure required members are set to avoid CS9035
                // FIX: Assign the masked textbox values to Model.Phone and Model.PostalCode as strings
                if (Model != null)
                {
                    if (phoneTextBox != null)
                    {
                        Model.Phone = phoneTextBox.Value;
                    }
                    if (zipTextBox != null)
                    {
                        Model.PostalCode = zipTextBox.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"AddEditBedRequest.OnInitializedAsync");
                _toastService?.Error(IsError, "An error occurred while initializing the Bed Request editor.");
            }
        }

        private async Task LoadConfiguration()
        {
            if (_svcConfiguration != null)
            {
                lstPrimaryLanguage = await _svcConfiguration.GetPrimaryLanguages();
                lstSpeakEnglish = await _svcConfiguration.GetSpeakEnglish();
            }
            else
            {
                lstPrimaryLanguage = new List<string>();
                lstSpeakEnglish = new List<string>();
            }
        }

        private async Task LoadLocations()
        {
            if (_svcLocation != null)
            {
                var result = await _svcLocation.GetActiveLocations();
                if (result.Success)
                {
                    Locations = result.Data?.ToList();
                }
            }
        }

        private async Task LoadModel()
        {
            if (BedRequestId.HasValue && BedRequestId.Value > 0)
            {
                if (_svcBedRequest != null)
                {
                    var result = await _svcBedRequest.GetByIdAsync(BedRequestId.Value);
                    if (result.Success && result.Data != null)
                    {
                        Model = result.Data;
                    }
                    else
                    {
                        Log.Error($"AddEditBedRequest, Error loading BedRequest {BedRequestId}: {result.Message}");
                        _toastService?.Error(IsError, result.Message);
                        Model = new BedBrigade.Common.Models.BedRequest { LocationId = LocationId };
                    }
                }
                else
                {
                    Log.Error("AddEditBedRequest, _svcBedRequest is null.");
                    _toastService?.Error(IsError, BRService);
                    Model = new BedBrigade.Common.Models.BedRequest { LocationId = LocationId };
                }
            }
            else
            {
                Model = new BedBrigade.Common.Models.BedRequest();
                Model.LocationId = LocationId;
                Model.PrimaryLanguage = "English";
                var location = Locations?.FirstOrDefault(o => o.LocationId == LocationId);
                if (location != null)
                {
                    Model.Group = location.Group;
                }
            }

            if (Model != null)
            {
                _originalStatus = Model.Status;
                _lastSelectedStatus = Model.Status;
            }
        }

        private void InitializeValidationContext()
        {
            if (_editContext != null)
            {
                _editContext.OnValidationRequested -= HandleValidationRequested;
                _editContext.OnFieldChanged -= HandleFieldChanged;
            }

            if (Model == null)
            {
                _editContext = null;
                _validationMessageStore = null;
                return;
            }

            _editContext = new EditContext(Model);
            _validationMessageStore = new ValidationMessageStore(_editContext);
            _editContext.OnValidationRequested += HandleValidationRequested;
            _editContext.OnFieldChanged += HandleFieldChanged;
        }

        private async Task LoadDeliverySchedules(int locationId)
        {
            FutureDeliverySchedules = new List<BedBrigade.Common.Models.Schedule>();

            if (locationId <= 0)
            {
                return;
            }

            var scheduleResponse = await _svcSchedule.GetFutureSchedulesByLocationId(locationId);

            if (!scheduleResponse.Success || scheduleResponse.Data == null)
            {
                return;
            }

            FutureDeliverySchedules = scheduleResponse.Data
                .Where(schedule => schedule.EventType == EventType.Delivery)
                .OrderBy(schedule => schedule.EventDateScheduled)
                .ToList();

            if (Model?.ScheduleId is int selectedScheduleId &&
                FutureDeliverySchedules.All(schedule => schedule.ScheduleId != selectedScheduleId))
            {
                Model.ScheduleId = null;
            }
        }

        // Fix CS8602: Add null checks before dereferencing _nav

        private async Task HandleValidSubmit()
        {
            if (Model == null)
                return;
                
            // Normalize phone
            if (!string.IsNullOrEmpty(Model.Phone))
            {
                Model.Phone = Model.Phone.FormatPhoneNumber();
            }

            // Set SpeakEnglish default if primary is English
            if (Model.PrimaryLanguage == "English")
            {
                Model.SpeakEnglish = "Yes";
            }
            else
            {
                if (String.IsNullOrEmpty(Model.SpeakEnglish))
                {
                    Model.SpeakEnglish = "No";
                }
            }

            // If combine duplicate happened and updated an existing record, just return to grid
            if (await CombineDuplicate(Model))
            {
                if (_nav != null)
                {
                    _nav.NavigateTo("/administration/manage/bedrequests");
                }
                return;
            }

            ApplyStatusTransitionRules(Model);

            if (Model.BedRequestId != 0)
            {
                await UpdateBedRequest(Model);
            }
            else 
            {
                await CreateBedRequest(Model);
            }

            // After save navigate back to main grid page
            if (_nav != null)
            {
                _nav.NavigateTo("/administration/manage/bedrequests");
            }
        }

        private async Task<bool> CombineDuplicate(BedBrigade.Common.Models.BedRequest bedRequest)
        {
            if (bedRequest.BedRequestId > 0 || bedRequest.Status != BedRequestStatus.Waiting || String.IsNullOrEmpty(bedRequest.Phone))
            {
                return false;
            }

            BedBrigade.Common.Models.BedRequest existingBedRequest = null;

            // FIX CS8602: Add null check for _svcBedRequest before dereferencing
            if (_svcBedRequest != null)
            {
                var existingByPhone = await _svcBedRequest.GetWaitingByPhone(bedRequest.Phone);

                if (existingByPhone.Success && existingByPhone.Data != null)
                {
                    existingBedRequest = existingByPhone.Data;
                }
                else if (!String.IsNullOrEmpty(bedRequest.Email))
                {
                    var existingByEmail = await _svcBedRequest.GetWaitingByEmail(bedRequest.Email);

                    if (existingByEmail.Success && existingByEmail.Data != null)
                    {
                        existingBedRequest = existingByEmail.Data;
                    }
                }
            }
            else
            {
                Log.Error("CombineDuplicate, _svcBedRequest is null.");
                _toastService?.Error(IsError, BRService);
                return false;
            }

            if (existingBedRequest == null)
            {
                return false;
            }
            existingBedRequest.UpdateDuplicateFields(bedRequest, $"Updated on {DateTime.Now.ToShortDateString()} by {_svcAuth?.UserName}.");

            var updateResult = await _svcBedRequest.UpdateAsync(existingBedRequest);

            if (updateResult.Success && updateResult.Data != null)
            {
                _toastService?.Warning("Update Successful", "A duplicate Bed Request with the same phone number or email was updated.");
                // Copy updated values to current model
                ObjectUtil.CopyProperties(updateResult.Data, bedRequest);
                return true;
            }

            Log.Error($"Unable to update BedRequest {Model?.BedRequestId} : {updateResult.Message}");
            _toastService?.Error("Update Unsuccessful", "The BedRequest was not updated successfully");

            return false;
        }

        private async Task CreateBedRequest(BedBrigade.Common.Models.BedRequest BedRequest)
        {
            if (_svcBedRequest == null)
            {
                Log.Error("CreateBedRequest, _svcBedRequest is null.");
                _toastService?.Error(IsError, BRService);
                return;
            }

            var result = await _svcBedRequest.CreateAsync(BedRequest);
            if (result.Success)
            {
                // set to returned object
                Model = result.Data;
                InitializeValidationContext();
            }
            if (Model != null && Model.BedRequestId != 0)
            {
                _originalStatus = Model.Status;
                _lastSelectedStatus = Model.Status;
                _toastService?.Success("BedRequest Created", "BedRequest Created Successfully!");
            }
            else
            {
                Log.Error($"Unable to create BedRequest : {result.Message}");
                _toastService?.Error("BedRequest Not Created", "BedRequest was not created successfully");
            }
        }

        private async Task UpdateBedRequest(BedBrigade.Common.Models.BedRequest bedRequest)
        {
            if (_svcBedRequest == null)
            {
                Log.Error("UpdateBedRequest, _svcBedRequest is null.");
                _toastService?.Error(IsError, BRService);
                return;
            }

            var updateResult = await _svcBedRequest.UpdateAsync(bedRequest);

            if (updateResult.Success && updateResult.Data != null)
            {
                Model = updateResult.Data;
                InitializeValidationContext();
                _originalStatus = Model.Status;
                _lastSelectedStatus = Model.Status;

                await _emailQueueDataService.DeleteQueuedByBedRequestId(Model.BedRequestId);
                await _smsQueueDataService.DeleteQueuedSmsByBedRequestId(Model.BedRequestId);
                
                if (Model.Status == BedRequestStatus.Scheduled)
                {
                    if (!Model.DeliveryDate.HasValue)
                    {
                        Log.Error($"BedRequest {Model.BedRequestId} is scheduled but DeliveryDate is null.");
                        _toastService?.Error("Delivery Date Required", "A scheduled BedRequest must have a delivery date before delivery reminders can be queued.");
                        return;
                    }

                    var scheduleResult = await GetOrCreateScheduleForBedRequestDeliveryDate(Model);
                    if (scheduleResult.Success && scheduleResult.Data != null)
                    {
                        //@@@HERE The email is not queued at all
                        await SendDeliveryReminderEmail(Model, scheduleResult.Data);

                        //@@@HERE The SMS is queued but the body is blank
                        await SendDeliveryReminderSms(Model, scheduleResult.Data);
                    }
                }
                
                _toastService?.Success("Update Successful", "The BedRequest was updated successfully");
            }
            else
            {
                Log.Error($"Unable to update BedRequest {bedRequest.BedRequestId} : {updateResult.Message}");
                _toastService?.Error("Update Unsuccessful", "The BedRequest was not updated successfully");
            }
        }

        private async Task SendDeliveryReminderSms(Common.Models.BedRequest model, Common.Models.Schedule scheduleResultData)
        {
            ServiceResponse<bool> smsResult = await _sendSmsLogic.QueueDeliverySmsReminder(model, scheduleResultData);
            if (!smsResult.Success)
            {
                Log.Error($"Failed to queue delivery reminder SMS for BedRequest {model.BedRequestId} : {smsResult.Message}");
                _toastService?.Error("SMS Not Queued", "The delivery reminder SMS was not queued successfully");
            }
        }

        private Task<bool> ConfirmAddScheduleAsync(string dialogTitle, string dialogMessage)
        {
            _confirmAddScheduleTitle = dialogTitle;
            _confirmAddScheduleMessage = dialogMessage;
            _showConfirmAddScheduleDialog = true;
            _confirmAddScheduleTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            StateHasChanged();
            return _confirmAddScheduleTcs.Task;
        }

        private void HandleConfirmAddScheduleSelection(bool isConfirmed)
        {
            _showConfirmAddScheduleDialog = false;
            _confirmAddScheduleTcs?.TrySetResult(isConfirmed);
            _confirmAddScheduleTcs = null;
            StateHasChanged();
        }

        private void HandleConfirmAddScheduleClose()
        {
            HandleConfirmAddScheduleSelection(false);
        }

        private async Task<ServiceResponse<Common.Models.Schedule>> GetOrCreateScheduleForBedRequestDeliveryDate(
            Common.Models.BedRequest model)
        {
            if (!model.DeliveryDate.HasValue)
            {
                Log.Error($"BedRequest {model.BedRequestId} is missing a delivery date.");
                _toastService?.Error("Delivery Date Required", "A delivery date is required before delivery reminders can be queued.");
                return new ServiceResponse<Common.Models.Schedule>("BedRequest DeliveryDate is null");
            }

            ServiceResponse<Common.Models.Schedule> scheduleResponse =
                await _svcSchedule.GetScheduleForBedRequestDeliveryDate(model);

            if (!scheduleResponse.Success || scheduleResponse.Data == null)
            {
                if (await ConfirmAddScheduleAsync("Schedule Not Found",
                        $"No schedule was found for the delivery date {model.DeliveryDate.Value.ToShortDateString()} of this Bed Request. Would you like to add a schedule for this delivery date?"))
                {
                    scheduleResponse = await _svcSchedule.AddMissingScheduleForBedRequestDeliveryDate(model);
                    
                    if (scheduleResponse.Success && scheduleResponse.Data != null)
                    {
                        model.ScheduleId = scheduleResponse.Data.ScheduleId;
                        await UpdateBedRequest(model);
                    }
                }
            }

            if (!scheduleResponse.Success || scheduleResponse.Data == null)
            {
                _toastService?.Error("Schedule Not Found",
                    "No schedule was found for the delivery date of this Bed Request, and a new schedule was not be created. The delivery reminder email was not queued.");
            }

            return scheduleResponse;
        }

        private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args)
        {
            if (Model == null || _validationMessageStore == null)
            {
                return;
            }

            ApplyScheduledDeliveryDateValidation(Model);
        }

        private void HandleFieldChanged(object? sender, FieldChangedEventArgs args)
        {
            if (Model == null || _editContext == null || _validationMessageStore == null)
            {
                return;
            }

            if (args.FieldIdentifier.FieldName != nameof(BedBrigade.Common.Models.BedRequest.DeliveryDate) &&
                args.FieldIdentifier.FieldName != nameof(BedBrigade.Common.Models.BedRequest.Status))
            {
                return;
            }

            ApplyScheduledDeliveryDateValidation(Model);
            _editContext.NotifyValidationStateChanged();
        }

        private void ApplyScheduledDeliveryDateValidation(BedBrigade.Common.Models.BedRequest bedRequest)
        {
            if (_validationMessageStore == null)
            {
                return;
            }

            var fieldIdentifier = new FieldIdentifier(bedRequest, nameof(BedBrigade.Common.Models.BedRequest.DeliveryDate));
            _validationMessageStore.Clear(fieldIdentifier);

            if (bedRequest.Status == BedRequestStatus.Scheduled && !bedRequest.DeliveryDate.HasValue)
            {
                const string validationMessage = "A scheduled BedRequest must have a delivery date before it can be saved.";
                _validationMessageStore.Add(fieldIdentifier, validationMessage);
            }
        }

        public void Dispose()
        {
            if (_editContext == null)
            {
                return;
            }

            _editContext.OnValidationRequested -= HandleValidationRequested;
            _editContext.OnFieldChanged -= HandleFieldChanged;
        }

        private async Task SendDeliveryReminderEmail(Common.Models.BedRequest model, Common.Models.Schedule schedule)
        {
            ServiceResponse<bool> emailResult = await _svcEmailBuilder.QueueDeliveryEmailReminder(model, schedule);
            if (!emailResult.Success)
            {
                Log.Error($"Failed to queue delivery reminder email for BedRequest {model.BedRequestId} : {emailResult.Message}");
                _toastService?.Error("Email Not Queued", "The delivery reminder email was not queued successfully");
            }
        }


        private void HandleCancel()
        {
            if (_nav != null)
            {
                _nav.NavigateTo("/administration/manage/bedrequests");
            }
        }

        public void OnLanguageChange(ChangeEventArgs<string, string> args)
        {
            SpeakEnglishVisibility = "hidden";
            if (args.Value != null)
            {
                if (args.Value.ToString() != "English")
                {
                    SpeakEnglishVisibility = "visible";
                }
            }
        }

        public void OnStatusChange(ChangeEventArgs<BedRequestStatus, BedRequestEnumItem> args)
        {
            if (Model == null)
            {
                return;
            }

            var previousStatus = _lastSelectedStatus ?? Model.Status;
            Model.Status = args.Value;

            if (ShouldClearScheduleAndDeliveryDate(previousStatus, args.Value))
            {
                Model.ScheduleId = null;
                Model.DeliveryDate = null;
            }
            else if (args.Value == BedRequestStatus.Scheduled)
            {
                SyncScheduleIdFromDeliveryDate();
            }

            _lastSelectedStatus = args.Value;
        }

        public void OnDeliveryDateChange(ChangedEventArgs<DateTime?> args)
        {
            if (Model == null)
            {
                return;
            }

            Model.DeliveryDate = args.Value;
            SyncScheduleIdFromDeliveryDate();
        }

        private void SyncScheduleIdFromDeliveryDate()
        {
            if (Model == null || Model.Status != BedRequestStatus.Scheduled)
            {
                return;
            }

            if (!Model.DeliveryDate.HasValue)
            {
                Model.ScheduleId = null;
                return;
            }

            var matchingSchedule = FutureDeliverySchedules
                .FirstOrDefault(schedule => schedule.EventType == EventType.Delivery &&
                                            schedule.EventDateScheduled.Date == Model.DeliveryDate.Value.Date);

            Model.ScheduleId = matchingSchedule?.ScheduleId;
        }

        private void ApplyStatusTransitionRules(BedBrigade.Common.Models.BedRequest bedRequest)
        {
            if (!ShouldClearScheduleAndDeliveryDate(_originalStatus, bedRequest.Status))
            {
                return;
            }

            bedRequest.ScheduleId = null;
            bedRequest.DeliveryDate = null;
        }

        private static bool ShouldClearScheduleAndDeliveryDate(BedRequestStatus? originalStatus,
            BedRequestStatus currentStatus)
        {
            return originalStatus == BedRequestStatus.Scheduled &&
                   (currentStatus == BedRequestStatus.Waiting || currentStatus == BedRequestStatus.Cancelled);
        }

        public async Task HandlePhoneMaskFocus()
        {
            if (JS != null && phoneTextBox != null)
            {
                await JS.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", phoneTextBox.ID, 0);
            }
        }

        public async Task HandleZipMaskFocus()
        {
            if (JS != null && zipTextBox != null)
            {
                await JS.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", zipTextBox.ID, 0);
            }
        }

        public async Task OnLocationChange(ChangeEventArgs<int, Location> args)
        {
            if (args.Value > 0 && Model != null && Locations != null)
            {
                var location = Locations.FirstOrDefault(o => o.LocationId == args.Value);

                if (location != null)
                {
                    Model.Group = location.Group;
                }

                Model.LocationId = args.Value;
                Model.ScheduleId = null;
                await LoadDeliverySchedules(args.Value);
                StateHasChanged();
            }
        }

        public void OnScheduleChange(ChangeEventArgs<int?, BedBrigade.Common.Models.Schedule> args)
        {
            if (Model == null)
            {
                return;
            }

            Model.ScheduleId = args.Value;

            if (!args.Value.HasValue)
            {
                return;
            }

            var selectedSchedule = FutureDeliverySchedules
                .FirstOrDefault(schedule => schedule.ScheduleId == args.Value.Value);

            if (selectedSchedule != null)
            {
                Model.DeliveryDate = selectedSchedule.EventDateScheduled;
            }
        }
    }
}
