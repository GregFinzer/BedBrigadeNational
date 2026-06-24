using System.Security.Claims;
using BedBrigade.Common.Constants;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace BedBrigade.Data.Services
{
    /// <summary>
    /// Store and manage the current user's authentication state as a browser Session JWT and in Server Side Blazor
    /// </summary>
    public class AuthService : IAuthService
    {
        const string AuthTokenName = "auth_token";
        private ClaimsPrincipal _currentUser;
        private readonly ICustomSessionService _sessionService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public event Func<ClaimsPrincipal, Task>? AuthChanged;

        public AuthService(ICustomSessionService sessionService, IJwtTokenService jwtTokenService,
            IHttpContextAccessor httpContextAccessor)
        {
            _sessionService = sessionService;
            _jwtTokenService = jwtTokenService;
            _httpContextAccessor = httpContextAccessor;
            _currentUser = new();
        }

        public ClaimsPrincipal CurrentUser
        {
            get { return _currentUser; }
            set
            {
                _currentUser = value;
                NotifyAuthChangedAsync();
            }
        }

        public string Email
        {
            get
            {
                ClaimsPrincipal? user = GetAuthenticatedUser();
                return user?.FindFirstValue(ClaimTypes.Name) ?? Defaults.DefaultUserNameAndEmail;
            }
        }

        public string? UserRole
        {
            get
            {
                ClaimsPrincipal? user = GetAuthenticatedUser();
                return user?.FindFirstValue(ClaimTypes.Role);
            }
        }

        public string? UserRoute
        {
            get
            {
                ClaimsPrincipal? user = GetAuthenticatedUser();
                return user?.FindFirstValue("UserRoute");
            }
        }

        public string UserName
        {
            get
            {
                ClaimsPrincipal? user = GetAuthenticatedUser();
                return user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Defaults.DefaultUserNameAndEmail;
            }
        }

        public int LocationId
        {
            get
            {
                ClaimsPrincipal? user = GetAuthenticatedUser();
                if (user != null)
                {
                    string? locationId = user.FindFirstValue("LocationId");
                    if (int.TryParse(locationId, out int result))
                    {
                        return result;
                    }
                }

                return 0;
            }
        }

        private ClaimsPrincipal? GetAuthenticatedUser()
        {
            return GetAuthenticatedHttpContextUser()
                   ?? (CurrentUser.Identity?.IsAuthenticated == true ? CurrentUser : null);
        }

        private ClaimsPrincipal? GetAuthenticatedHttpContextUser()
        {
            ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true ? user : null;
        }

        public string TimeZoneId
        {
            get
            {
                ClaimsPrincipal? user = GetAuthenticatedUser();
                string? timeZoneId = user?.FindFirstValue("TimeZoneId");
                return string.IsNullOrEmpty(timeZoneId) ? Defaults.DefaultTimeZoneId : timeZoneId;
            }
        }

        public string Phone
        {
            get
            {
                ClaimsPrincipal? user = GetAuthenticatedUser();
                string? phone = user?.FindFirstValue("Phone");
                return string.IsNullOrEmpty(phone) ? Defaults.DefaultTimeZoneId : phone;
            }
        }

        public bool IsLoggedIn => GetAuthenticatedUser() != null;

        public bool IsNationalAdmin
        {
            get
            {
                return string.Equals(UserRole, RoleNames.NationalAdmin,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        public async Task LogoutAsync(bool removeFromBrowser)
        {
            bool wasLoggedIn = IsLoggedIn;
            string userName = UserName;
            string email = Email;

            //Update the Blazor Server State for the user to an anonymous user
            CurrentUser = new();

            if (removeFromBrowser)
            {
                //Remove the JWT from the browser session
                try
                {
                    string authToken = await _sessionService.GetItemAsStringAsync(AuthTokenName);

                    if (!string.IsNullOrEmpty(authToken))
                    {
                        await _sessionService.RemoveItemAsync(AuthTokenName);
                    }
                }
                catch (Exception)
                {
                    //This happens if the component is statically rendered
                }
            }

            if (wasLoggedIn)
            {
                Log.Logger.Information("User {Email} ({UserName}) logged out", email, userName);
            }
        }



        /// <summary>
        /// If the user somehow loses their server session, this method will attempt to restore the state from the JWT in the browser session
        /// </summary>
        /// <returns>True if the state was restored</returns>
        public async Task<bool> GetStateFromTokenAsync()
        {
            bool result = false;
            string authToken = await _sessionService.GetItemAsStringAsync(AuthTokenName);

            var identity = new ClaimsIdentity();

            if (!string.IsNullOrEmpty(authToken))
            {
                try
                {
                    ClaimsPrincipal principal = _jwtTokenService.ValidateToken(authToken);
                    identity = principal.Identity as ClaimsIdentity ?? new ClaimsIdentity(principal.Claims, "jwt");
                    result = true;
                }
                catch
                {
                    //If the JWT is invalid, remove it from the session
                    await _sessionService.RemoveItemAsync(AuthTokenName);

                    //This is an anonymous user
                    identity = new ClaimsIdentity();
                }
            }

            var user = new ClaimsPrincipal(identity);

            //Update the Blazor Server State for the user
            CurrentUser = user;
            return result;
        }


        public async Task Login(ClaimsPrincipal? user)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                throw new ArgumentException("User must be authenticated to login.");
            }

            //Update the Blazor Server State for the user
            CurrentUser = user;

            string jwt = _jwtTokenService.GenerateToken(user).Token;

            //Write a JWT to the browser session
            await _sessionService.SetItemAsStringAsync(AuthTokenName, jwt);
        }

        public async Task NotifyAuthChangedAsync()
        {
            if (AuthChanged != null)
            {
                foreach (Func<ClaimsPrincipal, Task> handler in AuthChanged.GetInvocationList())
                {
                    await handler.Invoke(_currentUser);
                }
            }
        }

        public bool UserHasRole(string roles)
        {
            ClaimsPrincipal? user = GetAuthenticatedUser();
            if (user == null)
            {
                return false;
            }

            var rolesToLookFor = roles.Split(',').Select(r => r.Trim());

            foreach (var role in rolesToLookFor)
            {
                if (user.IsInRole(role))
                {
                    return true;
                }
            }

            return false;
        }


    }


}
