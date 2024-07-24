using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client.Services
{
    public class DevTalkShow : AuthenticationStateProvider
    {
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(new []             {
                new Claim(ClaimTypes.Name, "DevTalkShow"),
            }, "devtalkshow");

            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
    }
}
