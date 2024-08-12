using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BedBrigade.Client.Components.Pages.Administration.Manage
{
    public partial class BedRequests : ComponentBase
    {
        [Inject] private IJSRuntime _js { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            //Collapse the mobile menu
            await _js.InvokeVoidAsync("AddRemoveClass.RemoveClass", "navbarResponsive", "show");
        }
    }
}
