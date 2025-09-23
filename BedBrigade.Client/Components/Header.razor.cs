using System.Globalization;
using System.Security.Claims;
using BedBrigade.Client.Services;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Services;
using BedBrigade.SpeakIt;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

using Microsoft.JSInterop;
using Serilog;

namespace BedBrigade.Client.Components
{
    public partial class Header : ComponentBase, IDisposable
    {
        // Client
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private NavigationManager _nm { get; set; }
        [Inject] private ILocationState _locationState { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private IContentTranslationDataService _svcContentTranslation { get; set; }

        [Inject] private ILanguageService _svcLanguage { get; set; }
        [Inject] private ITranslateLogic _translateLogic { get; set; }

        [Inject] private ILocalStorageService _localStorage { get; set; }
        const string LoginElement = "loginElement";
        const string AdminElement = "adminElement";
        const string SetInnerHTML = "SetGetValue.SetInnerHtml";

        private string headerContent = string.Empty;
        protected string Login = "login";
        private ClaimsPrincipal? User { get; set; }

        private string? _selectedCulture;
        public List<CultureInfo> Cultures { get; set; } = new List<CultureInfo>();

        public string? SelectedCulture
        {
            get => _selectedCulture;
            set
            {
                if (_selectedCulture != value)
                {
                    _selectedCulture = value;
                    _localStorage.SetItemAsync("language", value);
                    _svcLanguage.CurrentCulture = CultureInfo.GetCultureInfo(value);
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            Log.Debug("Header.OnInitializedAsync");
            SetupCulture();
            await LoadContent();
            _svcAuth.AuthChanged += OnAuthChanged;
            _locationState.OnChange += OnLocationChanged;
            _svcLanguage.LanguageChanged += OnLanguageChanged;
        }

        private void SetupCulture()
        {
            _lc.InitLocalizedComponent(this);
            if (Cultures.Count == 0)
            {
                Cultures = _translateLogic.GetRegisteredLanguages();
            }

            _selectedCulture = _lc.CurrentCulture.Name;
        }

        private async Task OnLanguageChanged(CultureInfo arg)
        {
            await LoadContent();
            StateHasChanged();
        }

        private Task OnAuthChanged(ClaimsPrincipal arg)
        {
            //We don't need to refresh if this is the first time to load the component
            if (User == null)
            {
                User = arg;
            }
            else
            {
                StateHasChanged();
            }

            return Task.CompletedTask;
        }

        public static string RemoveTextAfterParenthesis(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            int parenthesisIndex = input.IndexOf('(');
            return parenthesisIndex > 0 ? input.Substring(0, parenthesisIndex).Trim() : input;
        }

        private async Task OnLocationChanged()
        {
            await LoadContent();
            StateHasChanged();
        }

        private async Task LoadContent()
        {
            try
            {
                string locationName = _locationState.Location ?? SeedConstants.SeedNationalName;
                var locationResult = await _svcLocation.GetLocationByRouteAsync($"/{locationName.ToLower()}");

                if (!locationResult.Success && locationResult.Data != null)
                {
                    Log.Logger.Error($"Error loading location: {locationResult.Message}");
                }
                else
                {
                    if (_svcLanguage.CurrentCulture.Name == Defaults.DefaultLanguage)
                    {
                        await LoadDefaultContent(locationResult, locationName);
                    }
                    else
                    {
                        await LoadContentByLanguage(locationResult, locationName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error loading Header content: {ex.Message}");
            }
        }

        private async Task LoadContentByLanguage(ServiceResponse<Location> locationResult, string locationName)
        {
            var contentResult = await _svcContentTranslation.GetAsync("Header", locationResult.Data.LocationId,
                _svcLanguage.CurrentCulture.Name);

            if (contentResult.Success && contentResult.Data != null)
            {
                headerContent = contentResult.Data.ContentHtml;
            }
            else
            {
                await LoadDefaultContent(locationResult, locationName);
            }
        }

        private async Task LoadDefaultContent(ServiceResponse<Location> locationResult, string locationName)
        {
            if (locationResult.Data == null)
            {
                Log.Logger.Error($"Location not found for {locationName}");
                return;
            }

            var contentResult = await _svcContent.GetAsync("Header", locationResult.Data.LocationId);

            if (contentResult.Success && contentResult.Data != null)
            {
                headerContent = contentResult.Data.ContentHtml;
            }
            else
            {
                Log.Logger.Error(
                    $"Error loading Header for LocationId {locationResult.Data.LocationId}: {contentResult.Message}");
            }
        }

        public void Dispose()
        {
            _svcAuth.AuthChanged -= OnAuthChanged;
            _locationState.OnChange -= OnLocationChanged; // Unsubscribe from the event
            _svcLanguage.LanguageChanged -= OnLanguageChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                Log.Debug("Header.OnAfterRenderAsync");

                if (firstRender)
                {
                    await LanguageLoadedFromBrowser();
                }

                await HandleRender();
            }
            catch (Microsoft.JSInterop.JSDisconnectedException)
            {
                // Ignore the exception when the JS runtime is disconnected (e.g., during hot reload)
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Header.OnAfterRenderAsync: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> LanguageLoadedFromBrowser()
        {
            //Try to load from local storage
            string? browserLanguage = await _localStorage.GetItemAsync<string>("language");

            // If not found, try to get from the browser
            if (string.IsNullOrEmpty(browserLanguage))
            {
                browserLanguage = await _js.InvokeAsync<string>("BedBrigadeUtil.GetBrowserLocale");
            }

            if (string.IsNullOrEmpty(browserLanguage))
            {
                return false;
            }

            // Try to find a matching culture
            var matchingCulture =
                Cultures.FirstOrDefault(c => c.Name.Equals(browserLanguage, StringComparison.OrdinalIgnoreCase));

            if (matchingCulture != null)
            {
                _selectedCulture = browserLanguage;
                if (matchingCulture.Name != _svcLanguage.CurrentCulture.Name)
                {
                    _svcLanguage.CurrentCulture = matchingCulture;
                    return true;
                }
            }

            return false;
        }

        private async Task HandleRender()
        {
            Log.Debug("Header.HandleRender");

            try
            {
                if (_svcAuth.IsLoggedIn)
                {
                    await _js.InvokeVoidAsync(SetInnerHTML, LoginElement, "Logout");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/logout");
                    await _js.InvokeVoidAsync("DisplayToggle.Show", "administration");
                    await ShowMenuItems();
                }
                else
                {
                    await _js.InvokeVoidAsync(SetInnerHTML, LoginElement, "Login");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/login");
                    await _js.InvokeVoidAsync("DisplayToggle.HideByClass", "nadmin");
                }
            }
            catch (Microsoft.JSInterop.JSDisconnectedException)
            {
                // Ignore the exception when the JS runtime is disconnected (e.g., during hot reload)
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Header.HandleOtherRenders: {ex.Message}");
                throw;
            }
        }

        private async Task ShowMenuItems()
        {
            Log.Debug("Header.SetMenuItems");
            try
            {
                if (_svcAuth.CurrentUser.HasRole(RoleNames.NationalAdmin))
                {
                    await Show("nadmin");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.NationalEditor))
                {
                    await Show("neditor");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationAdmin))
                {
                    await Show("ladmin");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationEditor))
                {
                    await Show("leditor");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationAuthor))
                {
                    await Show("lauthor");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationScheduler))
                {
                    await Show("lscheduler");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationTreasurer))
                {
                    await Show("ltreasurer");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationCommunications))
                {
                    await Show("lcommunications");
                }
            }
            catch (Microsoft.JSInterop.JSDisconnectedException)
            {
                // Ignore the exception when the JS runtime is disconnected (e.g., during hot reload)
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error loading Menu: {ex.Message}");
            }

        }

        private async Task Show(string cssClass)
        {
            try
            {
                await _js.InvokeVoidAsync("DisplayToggle.ShowByClass", cssClass);
            }
            catch (Microsoft.JSInterop.JSDisconnectedException)
            {
                // Ignore the exception when the JS runtime is disconnected (e.g., during hot reload)
            }
        }



    }
}
