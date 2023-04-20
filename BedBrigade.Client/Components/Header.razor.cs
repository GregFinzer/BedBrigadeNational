using BedBrigade.Client.Pages.Administration;
using BedBrigade.Client.Services;
using BedBrigade.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace BedBrigade.Client.Components
{
    public partial class Header
    {
        // Client
        [Inject]
        private IJSRuntime _js { get; set; }

        [Inject]
        private IContentService _svcContent { get; set; }

        [Inject]
        private AuthenticationStateProvider _authState { get; set; }

        [Inject]
        private NavigationManager _nv { get; set; }

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
        public bool IsNationalAdmin { get; private set; }
        public bool IsNationalEditor { get; private set; }
        public bool IsLocationAdmin { get; private set; }
        public bool IsLocationAuthor { get; private set; }
        public bool IsLocationScheduler { get; private set; }
        public bool IsLocationTreasurer { get; private set; }
        public bool IsLocationEditor { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            var result = await _svcContent.GetAsync("Header");
            if (result.Success)
            {
                headerContent = result.Data.ContentHtml;
            }

            authState = await _authState.GetAuthenticationStateAsync();

            IsNationalAdmin = authState.User.HasRole(NationalAdmin);
            IsNationalEditor = authState.User.HasRole(NationalEditor);

            IsLocationAdmin = authState.User.HasRole(LocationAdmin);
            IsLocationAuthor = authState.User.HasRole(LocationAuthor);
            IsLocationScheduler = authState.User.HasRole(LocationScheduler);
            IsLocationTreasurer = authState.User.HasRole(LocationTreasurer);
            IsLocationEditor = authState.User.HasRole(LocationEditor);

            Menu = FindMenu();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (authState.User.HasRole($"{NationalAdmin}, {LocationAdmin}, {LocationAuthor}, {LocationScheduler}"))
                {
                    await _js.InvokeVoidAsync(SetInnerHTML, LoginElement, "Logout");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/home/logout");
                    await _js.InvokeVoidAsync("DisplayToggle.Toggle", "administration");
                    if (Menu == "dashboard")
                    {
                        await _js.InvokeVoidAsync("AddRemoveClass.SetClass", AdminElement, "dropdown-toggle active");
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
            var location = _nv.Uri.Split('/');
            return location[location.Length - 1];
        }
    }
}