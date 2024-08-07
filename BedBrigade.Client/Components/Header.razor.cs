using BedBrigade.Common;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Serilog;

namespace BedBrigade.Client.Components
{
    public partial class Header
    {
        // Client
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider _authenticationStateProvider { get; set; }
        [Inject] private NavigationManager _nm { get; set; }

        const string LoginElement = "loginElement";
        const string AdminElement = "adminElement";
        const string SetInnerHTML = "SetGetValue.SetInnerHtml";

        private string headerContent = string.Empty;
        protected string Login = "login";
        private bool IsAuthenicated { get; set; } = false;
        private string Menu { get; set; }
        private AuthenticationState _authState { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Log.Debug("Header.OnInitializedAsync");
            _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
            try
            {
                var contentResult = await _svcContent.GetAsync("Header", (int)LocationNumber.National);
                if (contentResult.Success)
                {
                    await Console.Out.WriteLineAsync($"Loaded Header");
                    headerContent = contentResult.Data.ContentHtml;
                }
                else
                {
                    Log.Logger.Error($"Error loading Header: {contentResult.Message}");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error loading header: {ex.Message}");
            }
        }

        private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        {
            _authState = await task;
            StateHasChanged();
        }

        public void Dispose()
        {
            _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            Log.Debug("Header.OnAfterRenderAsync");
            await HandleRender();
        }

        private async Task HandleRender()
        {
            Log.Debug("Header.HandleRender");

            try
            {
                _authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                if (_authState != null 
                    && _authState.User != null 
                    && _authState.User.Identity != null 
                    && _authState.User.Identity.IsAuthenticated)
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
                if (_authState.User.HasRole(RoleNames.NationalAdmin))
                {
                    await Show("nadmin");
                }
                else if (_authState.User.HasRole(RoleNames.NationalEditor))
                {
                    await Show( "neditor");
                }
                else if (_authState.User.HasRole(RoleNames.LocationAdmin))
                {
                    await Show( "ladmin");
                }
                else if (_authState.User.HasRole(RoleNames.LocationEditor))
                {
                    await Show( "leditor");
                }
                else if (_authState.User.HasRole(RoleNames.LocationAuthor))
                {
                    await Show( "lauthor");
                }
                else if (_authState.User.HasRole(RoleNames.LocationScheduler))
                {
                    await Show( "lscheduler");
                }
                else if (_authState.User.HasRole(RoleNames.LocationTreasurer))
                {
                    await Show( "ltreasurer");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error loading Menu: {ex.Message}");
            }
        
        }

        private async Task Show(string cssClass)
        {
            await _js.InvokeVoidAsync("DisplayToggle.ShowByClass", cssClass);
        }


    }
}