using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Inputs;
using System.Text.RegularExpressions;
using BedBrigade.Common.Models;
using Microsoft.JSInterop;
using Serilog;

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
        public EventCallback<string> LocationChanged { get; set; }        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] public required ILanguageContainerService _lc { get; set; }
        [Inject] public required IJSRuntime JS { get; set; }
        private List<LocationDistance> Locations { get; set; } = new List<LocationDistance>();

        public required SfMaskedTextBox maskObj;

        public int ddlValue { get; set; } = 0;

        private string? PostalCode { get; set; } = string.Empty;
        private string PostalCodeResult { get; set; } = string.Empty;
        private const string DisplayNone = "none";
        private string strAlertDisplay = DisplayNone;
        private string strDisplayNotActive = DisplayNone;
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

        protected override void OnInitialized()
        {
            _lc.InitLocalizedComponent(this);
        }

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
            else
            {
                Log.Error($"Failed to find location by name: {locationName}. Error: {result.Message}");
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
                PostalCodeResult = _lc.Keys["RequiredPostalCode"];
                return false;
            }
            if (!Regex.IsMatch(PostalCode, @"\d{5}$"))
            {
                strAlertType = "alert alert-danger";
                PostalCodeResult = _lc.Keys["InvalidPostalCode"];
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
            var result = await _svcLocation.GetBedBrigadeNearMe(PostalCode);
            StateHasChanged();
            IsSearching = false;

            if (!result.Success || result.Data == null)
            {
                strAlertType = AlertDanger;
                PostalCodeResult = result.Message;
                Locations = new List<LocationDistance>(); // empty location list
                await SetZipBoxFocus();
                return;
            } // some error


            await GetSearchResult(result);


        } // HandleSearchClick()

        private async Task GetSearchResult(ServiceResponse<List<LocationDistance>> result)
        {
            strDisplayNotActive = DisplayNone;

            if (result.Data.Count == 0)
            {
                strAlertType = "alert alert-warning";
                PostalCodeResult = result.Message;
                Locations = new List<LocationDistance>(); // empty location list
                await SetZipBoxFocus();
            }
            else
            {
                
                strAlertType = "alert alert-success";
                PostalCodeResult = result.Message;
                Locations = result.Data;
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

                // display location list - does contain not active?
                bool containsAsterisk = Locations.Any(location => location.Name.Contains("*"));
                if (containsAsterisk) {
                    strDisplayNotActive = "";
                }

                // Scroll to search results
                await JS.InvokeVoidAsync("BedBrigadeUtil.ScrollToElementId", "searchResultDisplay");


            } // Locations Found

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
        
        public async Task HandleMaskFocus()
        {
            await JS.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", maskObj.ID, 0);
        }

    }


}
