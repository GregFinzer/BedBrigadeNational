using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BedBrigade.Client.Services
{
    public class AuthServiceV8 : IAuthServiceV8
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthServiceV8(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LoginAsync(ClaimsPrincipal claimsPrincipal)
        {
            await _httpContextAccessor.HttpContext.SignInAsync(claimsPrincipal);
        }

        public async Task LogoutAsync()
        {
            //await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
