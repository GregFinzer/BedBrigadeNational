using System.ComponentModel;
using BedBrigade.Common;
using System.Diagnostics;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BedBrigade.Client.Components
{
    public partial class ReCAPTCHA
    {           

        [Parameter]
        public EventCallback<string> OnSuccess { get; set; }

        [Parameter]
        public EventCallback OnExpired { get; set; }

        public string SiteKey { get; set; }

        private string UniqueId = Guid.NewGuid().ToString();

        private int WidgetId;

        public string ResultPrint = String.Empty;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                SiteKey = ReCaptchaApi.GetReCaptchaKey("SiteKey");
                // Debug.WriteLine("Site Key: " + SiteKey);

                await JS.InvokeAsync<object>("My.reCAPTCHA.init");
                this.WidgetId = await JS.InvokeAsync<int>("My.reCAPTCHA.render", DotNetObjectReference.Create(this), UniqueId, SiteKey);
                //ResultPrint = "SiteKey OK";
            }
        }

        [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
        public void CallbackOnSuccess(string response)
        {
            if (OnSuccess.HasDelegate)
            {
                OnSuccess.InvokeAsync(response);
                //ResultPrint = response;

            }
        }

        [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
        public void CallbackOnExpired()
        {
            if (OnExpired.HasDelegate)
            {
                OnExpired.InvokeAsync(null);
            }
        }

        public ValueTask<string> GetResponseAsync()
        {
            return JS.InvokeAsync<string>("My.reCAPTCHA.getResponse", WidgetId);
        }

    }
}
