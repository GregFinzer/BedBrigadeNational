using BedBrigade.Common.Logic;

namespace BedBrigade.Tests;

[TestFixture]
public class LoginLockoutLogicTests
{
    [Test]
    public void BuildLockoutMessage_ShouldRoundUpRemainingMinutes()
    {
        DateTime now = new(2026, 5, 14, 12, 0, 0, DateTimeKind.Utc);
        DateTime lockoutEndUtc = now.AddMinutes(29).AddSeconds(1);

        string message = LoginLockoutLogic.BuildLockoutMessage(lockoutEndUtc, now);

        Assert.That(message, Is.EqualTo("Your account is locked for 30 minutes."));
    }

    [Test]
    public void BuildLockoutMessage_ShouldReturnMinimumOfOneMinute()
    {
        DateTime now = new(2026, 5, 14, 12, 0, 0, DateTimeKind.Utc);
        DateTime lockoutEndUtc = now.AddSeconds(1);

        string message = LoginLockoutLogic.BuildLockoutMessage(lockoutEndUtc, now);

        Assert.That(message, Is.EqualTo("Your account is locked for 1 minutes."));
    }

    [Test]
    public void IsLocked_ShouldReturnFalse_WhenLockoutHasExpired()
    {
        DateTime now = new(2026, 5, 14, 12, 0, 0, DateTimeKind.Utc);
        DateTime lockoutEndUtc = now.AddMinutes(-1);

        bool isLocked = LoginLockoutLogic.IsLocked(lockoutEndUtc, now);

        Assert.That(isLocked, Is.False);
    }
}

