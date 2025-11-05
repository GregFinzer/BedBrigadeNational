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
using Syncfusion.Blazor.Inputs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BedBrigade.Client.Components.Pages.Administration.AdminTasks
{
    public partial class AddEditBedRequest : ComponentBase
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
        [Parameter] public string? Id { get; set; }

        protected BedBrigade.Common.Models.BedRequest? Model { get; set; }

        protected List<Location>? Locations { get; set; }
        protected List<string>? lstPrimaryLanguage;
        protected List<string>? lstSpeakEnglish;
        protected List<BedRequestEnumItem>? BedRequestStatuses { get; private set; }
        private List<UsState>? StateList = AddressHelper.GetStateList();
        public required SfMaskedTextBox zipTextBox;
        public required SfMaskedTextBox phoneTextBox;

        private const string IsError = "Error";
        private const string BRService = "_svcBedRequest service is not available.";

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
        }

        // Fix CS8602: Add null checks before dereferencing _nav

        private async Task HandleValidSubmit()
        {
            // Normalize phone
            if (Model != null && !string.IsNullOrEmpty(Model.Phone))
            {
                Model.Phone = Model.Phone.FormatPhoneNumber();
            }

            // Set SpeakEnglish default if primary is English
            if (Model != null && Model.PrimaryLanguage == "English")
            {
                Model.SpeakEnglish = "Yes";
            }
            else
            {
                if (Model != null)
                {
                    if (String.IsNullOrEmpty(Model.SpeakEnglish))
                    {
                        Model.SpeakEnglish = "No";
                    }
                }
            }

            // If combine duplicate happened and updated an existing record, just return to grid
            if (Model != null && await CombineDuplicate(Model))
            {
                if (_nav != null)
                {
                    _nav.NavigateTo("/administration/manage/bedrequests");
                }
                return;
            }


            if (Model != null){
           
                    if (Model.BedRequestId != 0)
                    {
                        await UpdateBedRequest(Model);
                    }
                    else 
                    {
                        await CreateBedRequest(Model);
                    }
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
            }
            if (Model != null && Model.BedRequestId != 0)
            {
                _toastService?.Success("BedRequest Created", "BedRequest Created Successfully!");
            }
            else
            {
                Log.Error($"Unable to create BedRequest : {result.Message}");
                _toastService?.Error("BedRequest Not Created", "BedRequest was not created successfully");
            }
        }

        private async Task UpdateBedRequest(BedBrigade.Common.Models.BedRequest BedRequest)
        {
            if (_svcBedRequest == null)
            {
                Log.Error("UpdateBedRequest, _svcBedRequest is null.");
                _toastService?.Error(IsError, BRService);
                return;
            }

            var updateResult = await _svcBedRequest.UpdateAsync(BedRequest);

            if (updateResult.Success)
            {
                _toastService?.Success("Update Successful", "The BedRequest was updated successfully");
            }
            else
            {
                Log.Error($"Unable to update BedRequest {BedRequest.BedRequestId} : {updateResult.Message}");
                _toastService?.Error("Update Unsuccessful", "The BedRequest was not updated successfully");
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

        public void OnLocationChange(ChangeEventArgs<int, Location> args)
        {
            if (args.Value > 0 && Model != null && Locations != null)
            {
                var location = Locations.FirstOrDefault(o => o.LocationId == args.Value);

                if (location != null)
                {
                    Model.Group = location.Group;
                    StateHasChanged();
                }
            }
        }
    }
}
