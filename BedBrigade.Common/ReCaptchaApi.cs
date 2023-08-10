using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BedBrigade.Common
{
    public class ReCaptchaApi
    {
        private IHttpClientFactory HttpClientFactory { get; }

        public ReCaptchaApi(IHttpClientFactory httpClientFactory)
        {
            this.HttpClientFactory = httpClientFactory;
        }

        public async Task<(bool Success, string[] ErrorCodes)> Post(string reCAPTCHAResponse)
        {

            var url = "https://www.google.com/recaptcha/api/siteverify";
            var content = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                //{"secret", this.reCAPTCHAVerificationOptions.CurrentValue.Secret},
                { "secret", GetReCaptchaKey("Secret") },
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

        public static string GetReCaptchaKey(string KeyName)
        {
            // KeyName = Secret or SiteKey
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
            return config.GetSection("reCAPTCHA").GetSection(KeyName).Value;

        }

    } // class   

} // namespace
