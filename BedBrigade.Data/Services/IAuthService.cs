using System.Security.Claims;

namespace BedBrigade.Data.Services
{
    public interface IAuthService
    {
        ClaimsPrincipal CurrentUser { get; set; }
        bool IsLoggedIn { get; }
        int LocationId { get; }
        string UserName { get; }
        string Email { get; }
        string? UserRole { get; }
        string? UserRoute { get; }
        string TimeZoneId { get; }
        string Phone { get; }
        bool IsNationalAdmin { get; }

        Task<bool> GetStateFromTokenAsync();
        Task LogoutAsync(); 
        Task Login(ClaimsPrincipal user);

        event Func<ClaimsPrincipal, Task> AuthChanged;
        Task NotifyAuthChangedAsync();
        bool UserHasRole(string roles);
    }
}
