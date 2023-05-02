using BedBrigade.Client.Services;
using BedBrigade.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Routing.Patterns;
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


        // Roles
        const string NationalAdmin = "National Admin";
        const string NationalEditor = "National Editor";
        const string LocationAdmin = "Location Admin";
        const string LocationAuthor = "Location Author";
        const string LocationScheduler = "Location Scheduler";
        const string LocationTreasurer = "Location Treasurer";
        const string LocationEditor = "Location Editor";

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
            string[] routePath = _nm.Uri.Split('/');
            if (routePath[3] == ""|| routePath[3] == "Administration") routePath[3] = "National";
            var result = await _svcLocation.GetLocationByRouteAsync($"/{routePath[3]}");
            if (result.Success)
            {
                var contentResult = await _svcContent.GetAsync("Header", result.Data.LocationId);
                if (contentResult.Success)
                {
                    headerContent = contentResult.Data.ContentHtml;
                }
            }
            
            authState = await _authState.GetAuthenticationStateAsync();
            if (authState != null)
            {
                IsNationalAdmin = authState.User.HasRole(NationalAdmin);
                IsNationalEditor = authState.User.HasRole(NationalEditor);

                IsLocationAdmin = authState.User.HasRole(LocationAdmin);
                IsLocationAuthor = authState.User.HasRole(LocationAuthor);
                IsLocationScheduler = authState.User.HasRole(LocationScheduler);
                IsLocationTreasurer = authState.User.HasRole(LocationTreasurer);
                IsLocationEditor = authState.User.HasRole(LocationEditor);
            }
            Menu = FindMenu();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender && authState != null)
            {
                if (authState.User.HasRole($"{NationalAdmin}, {LocationAdmin}, {LocationAuthor}, {LocationScheduler}"))
                {
                    await _js.InvokeVoidAsync(SetInnerHTML, LoginElement, "Logout");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/home/logout");
                    await _js.InvokeVoidAsync("DisplayToggle.Toggle", "administration");
                    if (Menu.ToLower() == "dashboard")
                    {
                        await _js.InvokeVoidAsync("AddRemoveClass.SetClass", AdminElement, "active");
                    }
                }
                else
                {
                    await _js.InvokeVoidAsync(SetInnerHTML, LoginElement, "Login");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/home/login");
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