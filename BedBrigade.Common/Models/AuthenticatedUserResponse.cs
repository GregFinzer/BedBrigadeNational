namespace BedBrigade.Common.Models;

public class AuthenticatedUserResponse
{
    public bool IsAuthenticated { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string UserRoute { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int LocationId { get; set; }
}

