using System.Security.Claims;

namespace BedBrigade.Client.Services
{
    public interface IAuthServiceV8
    {
        Task LoginAsync(ClaimsPrincipal claimsPrincipal);
    }
}
