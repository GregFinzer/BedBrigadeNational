using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BedBrigade.Data.Services;
using Microsoft.IdentityModel.Tokens;
namespace BedBrigade.Client.Services;
public class UploadAuthorizationService : IUploadAuthorizationService
{
    private const string ContentNameClaim = "ContentName";
    private const string ContentTypeClaim = "ContentType";
    private const string IsNationalAdminClaim = "IsNationalAdmin";
    private const string LocationIdClaim = "LocationId";
    private const string UserLocationIdClaim = "UserLocationId";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(4);
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    public UploadAuthorizationService(IConfiguration configuration, IAuthService authService)
    {
        _configuration = configuration;
        _authService = authService;
    }
    public string CreateImageUploadToken(int locationId, string contentType, string contentName)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(locationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentName);
        if (!_authService.IsLoggedIn)
        {
            throw new InvalidOperationException("An authenticated user is required to create an upload token.");
        }
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(LocationIdClaim, locationId.ToString()),
                new Claim(ContentTypeClaim, contentType),
                new Claim(ContentNameClaim, contentName),
                new Claim(UserLocationIdClaim, _authService.LocationId.ToString()),
                new Claim(IsNationalAdminClaim, _authService.IsNationalAdmin.ToString())
            ],
            expires: DateTime.UtcNow.Add(TokenLifetime),
            signingCredentials: new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha512Signature));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public bool TryValidateImageUploadToken(string token, int locationId, string contentType, string contentName,
        out string errorMessage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(locationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentName);
        errorMessage = "Unauthorized image upload request.";
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = GetSigningKey(),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);
            string? tokenLocationId = principal.FindFirstValue(LocationIdClaim);
            string? tokenContentType = principal.FindFirstValue(ContentTypeClaim);
            string? tokenContentName = principal.FindFirstValue(ContentNameClaim);
            string? tokenUserLocationId = principal.FindFirstValue(UserLocationIdClaim);
            string? tokenIsNationalAdmin = principal.FindFirstValue(IsNationalAdminClaim);
            if (tokenLocationId != locationId.ToString()
                || !string.Equals(tokenContentType, contentType, StringComparison.Ordinal)
                || !string.Equals(tokenContentName, contentName, StringComparison.Ordinal)
                || !int.TryParse(tokenUserLocationId, out int userLocationId)
                || !bool.TryParse(tokenIsNationalAdmin, out bool isNationalAdmin))
            {
                return false;
            }
            if (!isNationalAdmin && userLocationId != locationId)
            {
                return false;
            }
            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
    private SecurityKey GetSigningKey()
    {
        string? tokenKey = _configuration.GetSection("AppSettings:Token").Value;
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenKey);
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
    }
}
