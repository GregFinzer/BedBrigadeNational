namespace BedBrigade.Common.Models;

/// <summary>
/// Represents the JWT token payload returned by the authentication API.
/// </summary>
public class AuthTokenResponse
{
    /// <summary>
    /// The JWT bearer token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The token type. This is typically <c>Bearer</c>.
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

}
