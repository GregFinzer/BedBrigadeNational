using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Inputs;
using System.Text.RegularExpressions;
using BedBrigade.Common.Models;

namespace BedBrigade.Client.Components
{
    public partial class SearchLocation : ComponentBase
    {
        [Parameter]
        public string? Title { get; set; }
        [Parameter]
        public string? ResultType { get; set; } // default is HyperlinkList, other found list of locations
        [Parameter]
        public EventCallback<string> ParentMethod { get; set; }
        [Parameter]
        public EventCallback<string> LocationChanged { get; set; }

        [Inject] private ILocationDataService? _svcLocation { get; set; }
        private List<LocationDistance> Locations { get; set; } = new List<LocationDistance>();

        SfMaskedTextBox maskObj;

        public int ddlValue { get; set; } = 0;

        public string InputID = "input-id";

        private string? PostalCode { get; set; } = string.Empty;
        private string PostalCodeResult { get; set; } = string.Empty;
        private const string DisplayNone = "none";
        private string strAlertDisplay = DisplayNone;
        private const string AlertDanger = "alert alert-danger";
        private string strAlertType = "alert alert-danger";
        private bool PostalCodeSuccess = false;
        private string SearchDisplay = "";
        private bool SubmitDisabled = true;
        private bool IsSearching = false;

        protected Dictionary<string, object> DropDownHtmlAttribute = new Dictionary<string, object>()
        {
           { "font-weight", "bold" },
        };

        public async Task ChangeLocation(ChangeEventArgs<int, LocationDistance> args)
        {
            await CallLocationChanged();
        }

        public async Task ForceLocationByName(string locationName)
        {
            string route = $"/{locationName}";
            var result = await _svcLocation.GetLocationByRouteAsync(route);
            if (result.Success && result.Data != null)
            {
                Locations = new List<LocationDistance>
                {
                    new LocationDistance()
                    {
                        Distance = 0,
                        LocationId = result.Data.LocationId,
                        Name = result.Data.Name,
                        Route = result.Data.Route
                    }
                };
                ddlValue = result.Data.LocationId;
                SearchDisplay = DisplayNone;
                ResultType = "DropDownList";
                StateHasChanged();
            }
        }
        
        public async Task OnCreateInput()
        {
            await SetZipBoxFocus();
        }


        public async Task SetZipBoxFocus()
        {
            await maskObj.FocusAsync();
            SubmitDisabled = true;
        }

        async Task CallLocationChanged()
        {
            try
            {
                await LocationChanged.InvokeAsync(ddlValue.ToString());
            }
            catch (Exception ex)
            {

            }
        }

        async Task CallParentMethod()
        {
            try
            {
                await ParentMethod.InvokeAsync(PostalCode);
            }
            catch (Exception ex)
            {

            }
        }

        private bool ValidatePostalCode()
        {

            if (string.IsNullOrEmpty(PostalCode))
            {
                strAlertType = "alert alert-warning";
                PostalCodeResult = "Warning! Please enter a zip code"; // warning
                return false;
            }
            if (!Regex.IsMatch(PostalCode, @"\d{5}$"))
            {
                strAlertType = "alert alert-danger";
                PostalCodeResult = "Error! Zip code must be numeric and have a length of 5.";
                return false;
            }

            PostalCodeResult = null;
            return true;
        }



        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Code == "Enter" || e.Code == "NumpadEnter")
            {
                StateHasChanged();
                await HandleSearchClick();
            }
        } // HandleKeyDown

        private async Task HandleSearchClick()
        {
            strAlertDisplay = "";

            if (!ValidatePostalCode())
                return;

            IsSearching = true;
            var Result = await _svcLocation.GetBedBrigadeNearMe(PostalCode);
            StateHasChanged();
            IsSearching = false;

            if (!Result.Success || Result.Data == null)
            {
                strAlertType = AlertDanger;
                PostalCodeResult = "Error! " + Result.Message;
                Locations = new List<LocationDistance>(); // empty location list
                await SetZipBoxFocus();
                return;
            } // some error


            if (Result.Message != null && Result.Message.Length > 0)
            {
                await GetSearchResult(Result.Message, Result);
            }
            else
            {
                strAlertType = AlertDanger;
                PostalCodeResult = "Unknown Error!";
                Locations = new List<LocationDistance>(); // empty location list
                await SetZipBoxFocus();
            }


        } // HandleSearchClick()

        private async Task GetSearchResult(string ResultMessage, ServiceResponse<List<LocationDistance>> Result)
        {
            if (!ResultMessage.Contains("No locations found"))
            {

                strAlertType = "alert alert-success";
                PostalCodeResult = "Success! " + ResultMessage;
                Locations = Result.Data;
                PostalCodeSuccess = true;
                if (ResultType == "DropDownList")
                {
                    if (Locations != null && Locations.Count > 0)
                    {
                        ddlValue = Locations.FirstOrDefault().LocationId;
                        SearchDisplay = DisplayNone;
                        await CallParentMethod(); // call parent method
                                                  // open parent edit form
                    }
                }
            } // Locations Found
            else
            { // Locations not found
                strAlertType = "alert alert-warning";
                PostalCodeResult = ResultMessage;
                Locations = new List<LocationDistance>(); // empty location list
                await SetZipBoxFocus();
            }
        } // Search Result

        private void ZipCodeInputChange(ChangeEventArgs e)
        {
            PostalCode = String.Empty;
            SubmitDisabled = true;
            if (e.Value != null && e.Value.ToString().Length == 5)
            {
                var isNumber = e.Value.ToString().All(c => Char.IsNumber(c));
                if (isNumber)
                {
                    PostalCode = e.Value.ToString();
                    SubmitDisabled = false;
                }
            }
        }

    }
}
