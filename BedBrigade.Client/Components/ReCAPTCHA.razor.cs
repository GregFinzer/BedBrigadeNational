using System.ComponentModel;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Data.Services;

using BedBrigade.Common.Models;
using Serilog;

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
        [Inject] private ILanguageContainerService _lc { get; set; }

        public string? SiteKey { get; set; }

        private string UniqueId = Guid.NewGuid().ToString();


        public string ResultPrint = String.Empty;

        protected override void OnInitialized()
        {
            _lc.InitLocalizedComponent(this);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender)
                {
                    SiteKey = await _svcConfiguration.GetConfigValueAsync(ConfigSection.System, ConfigNames.ReCaptchaSiteKey);
                    await JS.InvokeAsync<object>("My.reCAPTCHA.init");
                    await JS.InvokeAsync<int>("My.reCAPTCHA.render", DotNetObjectReference.Create(this), UniqueId, SiteKey);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ReCAPTCHA component");
                throw;
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
                    
        public async Task<(bool Success, string[] ErrorCodes)> Post(string reCAPTCHAResponse)
        {
            try
            {
                var url = "https://www.google.com/recaptcha/api/siteverify";
                var content = new FormUrlEncodedContent(new Dictionary<string, string?>
                {
                    { "secret", SiteKey },
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
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error verifying reCAPTCHA response");
                return (Success: false, ErrorCodes: new[] { "An error occurred while verifying reCAPTCHA." });
            }

        } // post

    }


}
