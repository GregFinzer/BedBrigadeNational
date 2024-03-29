﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BedBrigade.Client.Pages
{
    public partial class BedRequest : ComponentBase
    {
        [Inject] private IJSRuntime _js { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            //Collapse the mobile menu
            await _js.InvokeVoidAsync("AddRemoveClass.RemoveClass", "navbarResponsive", "show");
        }
    }
}
