using BedBrigade.Common.Models;
using System.Security.Claims;

namespace BedBrigade.Data.Services;

public interface IJwtTokenService
{
    AuthTokenResponse GenerateToken(ClaimsPrincipal principal);
    ClaimsPrincipal ValidateToken(string token);
}
