using System.Security.Claims;
using BedBrigade.Common.Constants;
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
        public event Func<ClaimsPrincipal, Task>? AuthChanged;

        public AuthService(ICustomSessionService sessionService, IJwtTokenService jwtTokenService)
        {
            _sessionService = sessionService;
            _jwtTokenService = jwtTokenService;
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
                if (CurrentUser != null
                    && CurrentUser.Identity != null
                    && CurrentUser.Identity.IsAuthenticated)
                {
                    return CurrentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? Defaults.DefaultUserNameAndEmail;
                }

                return Defaults.DefaultUserNameAndEmail;
            }
        }

        public string? UserRole
        {
            get
            {
                if (CurrentUser != null
                    && CurrentUser.Identity != null
                    && CurrentUser.Identity.IsAuthenticated)
                {
                    return CurrentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                }

                return null;
            }
        }

        public string? UserRoute
        {
            get
            {
                if (CurrentUser != null
                    && CurrentUser.Identity != null
                    && CurrentUser.Identity.IsAuthenticated)
                {
                    return CurrentUser.Claims.FirstOrDefault(c => c.Type == "UserRoute")?.Value;
                }

                return null;
            }
        }

        public string UserName
        {
            get
            {
                if (CurrentUser != null
                    && CurrentUser.Identity != null
                    && CurrentUser.Identity.IsAuthenticated)
                {
                    return CurrentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
                }

                return Defaults.DefaultUserNameAndEmail;
            }
        }

        public int LocationId
        {
            get
            {
                if (CurrentUser != null
                    && CurrentUser.Identity != null
                    && CurrentUser.Identity.IsAuthenticated)
                {
                    string? locationId = CurrentUser.Claims.FirstOrDefault(c => c.Type == "LocationId")?.Value;
                    if (int.TryParse(locationId, out int result))
                    {
                        return result;
                    }
                }

                return 0;
            }
        }

        public string TimeZoneId
        {
            get
            {
                if (CurrentUser != null
                    && CurrentUser.Identity != null
                    && CurrentUser.Identity.IsAuthenticated)
                {
                    string? timeZoneId = CurrentUser.Claims.FirstOrDefault(c => c.Type == "TimeZoneId")?.Value;
                    if (!String.IsNullOrEmpty(timeZoneId))
                    {
                        return timeZoneId;
                    }
                }

                return Defaults.DefaultTimeZoneId;
            }
        }

        public string Phone
        {
            get
            {
                if (CurrentUser != null
                    && CurrentUser.Identity != null
                    && CurrentUser.Identity.IsAuthenticated)
                {
                    string? timeZoneId = CurrentUser.Claims.FirstOrDefault(c => c.Type == "Phone")?.Value;
                    if (!String.IsNullOrEmpty(timeZoneId))
                    {
                        return timeZoneId;
                    }
                }

                return Defaults.DefaultTimeZoneId;
            }
        }

        public bool IsLoggedIn => CurrentUser.Identity?.IsAuthenticated ?? false;

        public bool IsNationalAdmin
        {
            get
            {
                string roleName = UserRole ?? string.Empty;

                return roleName.ToLower() == RoleNames.NationalAdmin.ToLower();
            }
        }

        public async Task LogoutAsync()
        {
            bool wasLoggedIn = IsLoggedIn;
            string userName = UserName;
            string email = Email;

            //Update the Blazor Server State for the user to an anonymous user
            CurrentUser = new();

            //Remove the JWT from the browser session
            try
            {
                string authToken = await _sessionService.GetItemAsStringAsync(AuthTokenName);

                if (!string.IsNullOrEmpty(authToken))
                {
                    await _sessionService.RemoveItemAsync(AuthTokenName);
                }
            }
            catch (System.InvalidOperationException)
            {
                //This happens if the component is statically rendered
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
            if (!IsLoggedIn)
            {
                return false;
            }

            var rolesToLookFor = roles.Split(',').Select(r => r.Trim());

            foreach (var role in rolesToLookFor)
            {
                if (CurrentUser.IsInRole(role))
                {
                    return true;
                }
            }

            return false;
        }


    }


}
