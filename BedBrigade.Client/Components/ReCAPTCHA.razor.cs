using System.ComponentModel;
using System.Diagnostics;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http.HttpResults;
using BedBrigade.Data.Services;

using BedBrigade.Common.Models;

namespace BedBrigade.Client.Components
{
    public partial class ReCAPTCHA
    {           

        [Parameter]
        public EventCallback<string> OnSuccess { get; set; }

        [Parameter]
        public EventCallback OnExpired { get; set; }
        private IHttpClientFactory HttpClientFactory { get; }

        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }

        public string? SiteKey { get; set; }

        private string UniqueId = Guid.NewGuid().ToString();

        private int WidgetId;

        public string ResultPrint = String.Empty;

        private Dictionary<string, string?> dctConfiguration { get; set; } = new Dictionary<string, string?>();

        protected override async Task OnInitializedAsync()
        {
            var dataConfiguration = await _svcConfiguration.GetAllAsync(ConfigSection.System);
            if (dataConfiguration.Success && dataConfiguration != null)
            {
                dctConfiguration = dataConfiguration.Data.ToDictionary(keySelector: x => x.ConfigurationKey, elementSelector: x => x.ConfigurationValue);
            }
        } // Init


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                SiteKey = dctConfiguration[ConfigNames.ReCaptchaSiteKey].ToString();
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
                    
        public async Task<(bool Success, string[] ErrorCodes)> Post(string reCAPTCHAResponse)
        {

            var url = "https://www.google.com/recaptcha/api/siteverify";
            var content = new FormUrlEncodedContent(new Dictionary<string, string?>
            {                
                { "secret", dctConfiguration[ConfigNames.ReCaptchaSecret].ToString() },
                { "response", reCAPTCHAResponse}
            });

            var httpClient = this.HttpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var verificationResponse = await response.Content.ReadAsAsync<reCAPTCHAVerificationResponse>();
            if (verificationResponse.Success) return (Success: true, ErrorCodes: new string[0]);

            return (
                Success: false,
                ErrorCodes: verificationResponse.ErrorCodes.Select(err => err.Replace('-', ' ')).ToArray());
        } // post

    }
}
