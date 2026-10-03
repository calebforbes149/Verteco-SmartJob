using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Infrastructure.Identity;

namespace VertecoSmartJob.Tests.Infrastructure;

public class PasswordHashingServiceTests
{
    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var service = new PasswordHashingService();
        var user = new User { Email = "test@verteco.example" };

        var hash = service.Hash(user, "CorrectHorseBatteryStaple");
        user.SetPasswordHash(hash);

        Assert.True(service.Verify(user, "CorrectHorseBatteryStaple"));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var service = new PasswordHashingService();
        var user = new User { Email = "test@verteco.example" };

        var hash = service.Hash(user, "CorrectHorseBatteryStaple");
        user.SetPasswordHash(hash);

        Assert.False(service.Verify(user, "WrongPassword"));
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenNoPasswordHashIsSet()
    {
        var service = new PasswordHashingService();
        var user = new User { Email = "test@verteco.example" };

        Assert.False(service.Verify(user, "AnyPassword"));
    }

    [Fact]
    public void Hash_ProducesDifferentOutput_ForSameInputTwice()
    {
        // PasswordHasher salts internally, so the same password hashed twice
        // must never produce the same stored value — this is what makes rainbow-table attacks useless.
        var service = new PasswordHashingService();
        var user = new User { Email = "test@verteco.example" };

        var hash1 = service.Hash(user, "SamePassword123");
        var hash2 = service.Hash(user, "SamePassword123");

        Assert.NotEqual(hash1, hash2);
    }
}