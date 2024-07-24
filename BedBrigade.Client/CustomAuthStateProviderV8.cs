using System.Security.Claims;
using BedBrigade.Client.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace BedBrigade.Client
{
    public class CustomAuthStateProviderV8 : AuthenticationStateProvider
    {
        private AuthenticationState authenticationState;

        public CustomAuthStateProviderV8(AuthServiceV8 service)
        {
            authenticationState = new AuthenticationState(service.CurrentUser);

            service.UserChanged += (newUser) =>
            {
                authenticationState = new AuthenticationState(newUser);

                NotifyAuthenticationStateChanged(
                    Task.FromResult(new AuthenticationState(newUser)));
            };
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(authenticationState);
    }
}
