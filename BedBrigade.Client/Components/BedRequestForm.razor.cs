using Syncfusion.Blazor;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.DropDowns;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;
using ChangeEventArgs = Microsoft.AspNetCore.Components.ChangeEventArgs;
using BedBrigade.Data.Models;
using static BedBrigade.Common.Common;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Web;
using BedBrigade.Common;

namespace BedBrigade.Client.Components
{
    public partial class BedRequestForm : ComponentBase
    {
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IBedRequestDataService? _svcBedRequest { get; set; }
        private BedBrigade.Data.Models.BedRequest? newRequest;
        private List<UsState>? StateList = GetStateList();
        private string PostalCode { get; set; } = string.Empty;
        private string PostalCodeError { get; set; } = string.Empty;
        private string PostalCodeSuccess { get; set; } = string.Empty;
        private List<LocationDistance> Locations { get; set; } = new List<LocationDistance>();
        private LocationDistance? selectedLocation { get; set; }
        private string DisplayLocations = "none";
        private string DisplaySearch = "";
        public int NumericValue { get; set; } = 1;

        private string strSubmitAlert = string.Empty;
        private string strAlertDisplay = "none";
        private string strAlertType = "alert alert-danger";

        private string cssClass { get; set; } = "e-outline";
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "4" },
        };

        protected Dictionary<string, object> DropDownHtmlAttribute = new Dictionary<string, object>()
        {
           { "font-weight", "bold" },
        };

        protected Dictionary<string, object> htmlattributeSize = new Dictionary<string, object>()
        {
           { "maxlength", "2" },
        };



        protected override void OnInitialized()
        {
            newRequest = new BedBrigade.Data.Models.BedRequest();
            

        }

        private async Task SaveRequest()
        {
            newRequest.LocationId = selectedLocation.LocationId;
            newRequest.NumberOfBeds = NumericValue;
            //strSubmitAlert = newRequest.SpecialInstructions;
            var jsonString = JsonSerializer.Serialize(newRequest);
            Debug.Write(jsonString);

            try
            {
                var addResult = await _svcBedRequest.CreateAsync(newRequest);
                if (addResult.Success && addResult.Data != null)
                {
                    newRequest = addResult.Data; // added Request
                }

                if (newRequest != null && newRequest.BedRequestId > 0)
                {
                    DisplayLocations = "none";
                    strSubmitAlert = "New Bed Request #" + newRequest.BedRequestId.ToString() + " created Successfully!";
                    strAlertType = "alert alert-success";
                }
                else
                {
                    strSubmitAlert = "Warning! Unable to add new Bed Request!";
                    strAlertType = "alert alert-danger";
                }
            }
            catch (Exception ex)
            {
                strSubmitAlert = "Error! " + ex.Message;
            }

            strAlertDisplay = "";

        } // Save Request



        private bool ValidatePostalCode()
        {
            if (string.IsNullOrEmpty(PostalCode))
            {
                PostalCodeError = "Please enter a zip code";
                return false;
            }
            if (!Regex.IsMatch(PostalCode, @"\d{5}$"))
            {
                PostalCodeError = "Zip code must be numeric and have a length of 5.";
                return false;
            }

            PostalCodeError = null;
            return true;
        }

        private void HandleInputChange(ChangeEventArgs e)
        {
            PostalCode = e.Value.ToString();
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Code == "Enter" || e.Code == "NumpadEnter")
            {
                StateHasChanged();
                await HandleSearchClick();
            }
        }

        private async Task HandleSearchClick()
        {
            if (!ValidatePostalCode())
                return;

            var response = await _svcLocation.GetBedBrigadeNearMe(PostalCode);

            if (response.Success && response.Data != null)
            {
                PostalCodeSuccess = response.Message;
                Locations = response.Data;
                selectedLocation = Locations.FirstOrDefault();
                DisplayLocations = "";
                DisplaySearch = "none";
                newRequest.PostalCode = PostalCode;
                newRequest.State = GetZipState(StateList, Convert.ToInt32(PostalCode));

            }
            else
            {
                PostalCodeError = response.Message;
                Locations = new List<LocationDistance>();
            }
        }
      

    } // page class
} // namespace
