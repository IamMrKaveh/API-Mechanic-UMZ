using Infrastructure.Security.Models;

namespace Tests.Infrastructure.Security.Models;

public class RateLimitEntryTests
{
    [Fact]
    public void Defaults_AreEmptyKeyWindowZeroCountAndDefaultExpiry()
    {
        var sut = new RateLimitEntry();

        sut.Id.ShouldBe(0);
        sut.Key.ShouldBe(string.Empty);
        sut.WindowKey.ShouldBe(string.Empty);
        sut.Count.ShouldBe(0);
        sut.ExpiresAt.ShouldBe(default);
    }

    [Fact]
    public void Properties_RoundtripAssignedValues()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var sut = new RateLimitEntry
        {
            Id = 42,
            Key = "login:192.168.1.1",
            WindowKey = "2026-09-06T10:00",
            Count = 3,
            ExpiresAt = expiresAt
        };

        sut.Id.ShouldBe(42);
        sut.Key.ShouldBe("login:192.168.1.1");
        sut.WindowKey.ShouldBe("2026-09-06T10:00");
        sut.Count.ShouldBe(3);
        sut.ExpiresAt.ShouldBe(expiresAt);
    }

    [Fact]
    public void Entries_WithSameKeyButDifferentWindows_AreDistinguishable()
    {
        var first = new RateLimitEntry { Key = "otp:0912", WindowKey = "window-a", Count = 1, ExpiresAt = DateTime.UtcNow };
        var second = new RateLimitEntry { Key = "otp:0912", WindowKey = "window-b", Count = 1, ExpiresAt = DateTime.UtcNow };

        (first.Key == second.Key && first.WindowKey != second.WindowKey).ShouldBeTrue();
    }

    [Fact]
    public void Expiry_InThePast_IndicatesStaleWindow()
    {
        var sut = new RateLimitEntry { ExpiresAt = DateTime.UtcNow.AddMinutes(-1) };

        (sut.ExpiresAt < DateTime.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void Expiry_InTheFuture_IndicatesActiveWindow()
    {
        var sut = new RateLimitEntry { ExpiresAt = DateTime.UtcNow.AddMinutes(5) };

        (sut.ExpiresAt > DateTime.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void Count_AcceptsZeroAndIncrements()
    {
        var sut = new RateLimitEntry { Count = 0 };

        sut.Count++;
        sut.Count.ShouldBe(1);

        sut.Count = int.MaxValue;
        sut.Count.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void Key_SupportsLongCompositeValues()
    {
        var key = "ratelimit:" + new string('k', 250);

        var sut = new RateLimitEntry { Key = key, WindowKey = "w" };

        sut.Key.ShouldBe(key);
        sut.Key.Length.ShouldBeGreaterThan(200);
    }
}
