using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BedBrigade.Client.Components.Layout
{
    public partial class NoHeaderFooterLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private IAuthService AuthService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private ISmsState _smsState { get; set; }
        [Inject] private ToastService _toastService { get; set; }

        private ErrorBoundary errorBoundary;

        protected override async Task OnInitializedAsync()
        {
            _smsState.OnChange += OnSmsStateChange;
        }

        private async Task OnSmsStateChange(SmsQueue smsQueue)
        {
            if (!AuthService.IsLoggedIn)
                return;

            if (smsQueue.LocationId == AuthService.LocationId
                || (AuthService.LocationId == Defaults.NationalLocationId
                && smsQueue.LocationId == Defaults.GroveCityLocationId))
            {
                _toastService.Success($"SMS Message: {smsQueue.ContactName} {smsQueue.FromPhoneNumber}",
                    smsQueue.Body);
                await _js.InvokeVoidAsync("BedBrigadeUtil.playNotification");
            }
        }

        public void Dispose()
        {
            _smsState.OnChange -= OnSmsStateChange;
        }

        protected override void OnParametersSet()
        {
            errorBoundary?.Recover();
            base.OnParametersSet();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // A hard refresh or the user just navigated to the application for the first time
            if (firstRender && !AuthService.IsLoggedIn)
            {
                var url = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).ToString();
                if (url.Contains("/login"))
                {
                    return;
                }

                var restoredFromState = await AuthService.GetStateFromTokenAsync();
                //The user lost their server side session, but still has a valid JWT
                if (restoredFromState)
                {
                    NavigationManager.Refresh();
                }
            }


            //Collapse the mobile menu
            await _js.InvokeVoidAsync("AddRemoveClass.RemoveClass", "navbarResponsive", "show");
        }
    }
}
