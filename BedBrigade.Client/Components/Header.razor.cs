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
        const string LocationAdmin = "Location Admin";
        const string LocationAuthor = "Location Author";
        const string LocationScheduler = "Location Scheduler";
        const string LoginElement = "loginElement";
        private string headerContent = string.Empty;
        protected string Login = "login";
        private ClaimsPrincipal Identity { get; set; }

        private bool IsAuthenicated { get; set; } = false;
        private string Menu { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var result = await _svcContent.GetAsync("Header");
            if (result.Success)
            {
                headerContent = result.Data.ContentHtml;
            }

            Menu = FindMenu();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                var authState = await _authState.GetAuthenticationStateAsync();
                if (authState.User.HasRole($"{NationalAdmin}, {LocationAdmin}, {LocationAuthor}, {LocationScheduler}"))
                {
                    await _js.InvokeVoidAsync("SetGetValue.SetInnerHtml", LoginElement, "Logout");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/home/logout");
                }
                else
                {
                    await _js.InvokeVoidAsync("SetGetValue.SetInnerHtml", LoginElement, "Login");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/home/login");
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