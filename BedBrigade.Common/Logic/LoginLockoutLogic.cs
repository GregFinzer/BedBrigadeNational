namespace BedBrigade.Common.Logic;

public static class LoginLockoutLogic
{
    private const string LockoutMessageFormat = "Your account is locked for {0} minutes.";

    public static bool IsLocked(DateTime? lockoutEndUtc, DateTime? currentUtc = null)
    {
        if (!lockoutEndUtc.HasValue)
        {
            return false;
        }

        DateTime now = currentUtc ?? DateTime.UtcNow;
        return lockoutEndUtc.Value > now;
    }

    public static int GetMinutesLeft(DateTime lockoutEndUtc, DateTime? currentUtc = null)
    {
        DateTime now = currentUtc ?? DateTime.UtcNow;
        TimeSpan remaining = lockoutEndUtc - now;
        return Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
    }

    public static string BuildLockoutMessage(DateTime lockoutEndUtc, DateTime? currentUtc = null)
    {
        int minutesLeft = GetMinutesLeft(lockoutEndUtc, currentUtc);
        return string.Format(LockoutMessageFormat, minutesLeft);
    }
}

