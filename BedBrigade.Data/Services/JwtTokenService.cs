using BedBrigade.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BedBrigade.Data.Services;

public class JwtTokenService : IJwtTokenService
{
    private const string TokenConfigurationKey = "AppSettings:Token";
    private const string TokenHoursConfigurationKey = "AppSettings:TokenHours";
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public AuthTokenResponse GenerateToken(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new ArgumentException("Principal must be authenticated to generate a token.", nameof(principal));
        }

        DateTime expiresAtUtc = DateTime.UtcNow.AddHours(GetTokenHours());
        var token = new JwtSecurityToken(
            claims: principal.Claims,
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha512Signature));

        return new AuthTokenResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token)
        };
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token is required.", nameof(token));
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.ValidateToken(token, CreateTokenValidationParameters(), out SecurityToken validatedToken);

        var jwtToken = (JwtSecurityToken)validatedToken;
        var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
        return new ClaimsPrincipal(identity);
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        string tokenEncryptionKey = _configuration[TokenConfigurationKey]
            ?? throw new InvalidOperationException($"Missing configuration value '{TokenConfigurationKey}'.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenEncryptionKey));
    }

    private int GetTokenHours()
    {
        string tokenHoursValue = _configuration[TokenHoursConfigurationKey]
            ?? throw new InvalidOperationException($"Missing configuration value '{TokenHoursConfigurationKey}'.");

        if (!int.TryParse(tokenHoursValue, out int tokenHours) || tokenHours <= 0)
        {
            throw new InvalidOperationException($"Configuration value '{TokenHoursConfigurationKey}' must be a positive integer.");
        }

        return tokenHours;
    }

    private TokenValidationParameters CreateTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSigningKey(),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    }
}
