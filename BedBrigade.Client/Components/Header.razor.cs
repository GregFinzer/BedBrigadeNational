using BedBrigade.Client.Services;
using BedBrigade.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace BedBrigade.Client.Components
{
    public partial class Header
    {
        // Client
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private IContentService _svcContent { get; set; }
        [Inject] private ILocationService _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider _authState { get; set; }
        [Inject] private NavigationManager _nm { get; set; }

        const string LoginElement = "loginElement";
        const string AdminElement = "adminElement";
        const string SetInnerHTML = "SetGetValue.SetInnerHtml";

        private string headerContent = string.Empty;
        protected string Login = "login";
        private bool IsAuthenicated { get; set; } = false;
        private string Menu { get; set; }
        private AuthenticationState authState { get; set; }
        public bool IsNationalAdmin { get; private set; } = false;
        public bool IsNationalEditor { get; private set; } = false;
        public bool IsLocationAdmin { get; private set; } = false;
        public bool IsLocationAuthor { get; private set; } = false;
        public bool IsLocationScheduler { get; private set; } = false;
        public bool IsLocationTreasurer { get; private set; } = false;
        public bool IsLocationEditor { get; private set; } = false;

        protected override async Task OnInitializedAsync()
        {
            string[] routePath = _nm.Uri.ToLower().Split('/');
            if (routePath[3] == "" || routePath[3] == "administration" || routePath[3] == "bed-brigade-near-me")
            {
                routePath[3] = "national";
            }

            var result = await _svcLocation.GetLocationByRouteAsync($"/{routePath[3]}");
            if (result.Success)
            {
                var contentResult = await _svcContent.GetAsync("Header", result.Data.LocationId);
                if (contentResult.Success)
                {
                    await Console.Out.WriteLineAsync($"loaded locations header for loction id {result.Data.LocationId}");
                    headerContent = contentResult.Data.ContentHtml;
                }

            }
            
            authState = await _authState.GetAuthenticationStateAsync();
            if (authState != null)
            {
                IsNationalAdmin = authState.User.HasRole(RoleNames.NationalAdmin);
                IsNationalEditor = authState.User.HasRole(RoleNames.NationalEditor);

                IsLocationAdmin = authState.User.HasRole(RoleNames.LocationAdmin);
                IsLocationAuthor = authState.User.HasRole(RoleNames.LocationAuthor);
                IsLocationScheduler = authState.User.HasRole(RoleNames.LocationScheduler);
                IsLocationTreasurer = authState.User.HasRole(RoleNames.LocationTreasurer);
                IsLocationEditor = authState.User.HasRole(RoleNames.LocationEditor);
            }
            Menu = FindMenu();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender && authState != null)
            {
                if (authState.User.HasRole($"{RoleNames.NationalAdmin}, {RoleNames.LocationAdmin}, {RoleNames.LocationAuthor}, {RoleNames.LocationScheduler}"))
                {
                    await _js.InvokeVoidAsync(SetInnerHTML, LoginElement, "Logout");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/logout");
                    await _js.InvokeVoidAsync("DisplayToggle.Toggle", "administration");
                    if (Menu.ToLower() == "dashboard")
                    {
                        await _js.InvokeVoidAsync("AddRemoveClass.SetClass", AdminElement, "active");
                    }
                }
                else
                {
                    await _js.InvokeVoidAsync(SetInnerHTML, LoginElement, "Login");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/login");
                    await _js.InvokeVoidAsync("AddRemoveClass.RemoveClass", AdminElement, "dropdown-toggle");
                }
            }
        }

        protected string FindMenu()
        {
            var location = _nm.Uri.Split('/');
            return location[location.Length - 1];
        }
    }
}