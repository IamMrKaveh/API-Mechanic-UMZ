using Infrastructure.Common.Services;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.Common.Services;

public class DateTimeProviderTests
{
    [Fact]
    public void ImplementsIDateTimeProvider()
    {
        var sut = new DateTimeProvider();

        sut.ShouldBeAssignableTo<IDateTimeProvider>();
    }

    [Fact]
    public void UtcNow_ReturnsValueWithUtcKind()
    {
        var sut = new DateTimeProvider();

        var value = sut.UtcNow;

        value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void UtcNow_ReturnsValueCloseToSystemUtcNow()
    {
        var sut = new DateTimeProvider();
        var before = DateTime.UtcNow;

        var value = sut.UtcNow;

        var after = DateTime.UtcNow;
        value.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
        value.ShouldBeLessThanOrEqualTo(after.AddSeconds(1));
    }

    [Fact]
    public void UtcNow_TwoConsecutiveReads_AreMonotonicallyNonDecreasing()
    {
        var sut = new DateTimeProvider();

        var first = sut.UtcNow;
        var second = sut.UtcNow;

        second.ShouldBeGreaterThanOrEqualTo(first);
    }

    [Fact]
    public void Today_ReturnsDateOnlyDerivedFromUtcNow()
    {
        var sut = new DateTimeProvider();

        var today = sut.Today;

        var expected = DateOnly.FromDateTime(DateTime.UtcNow);
        (today == expected || today == expected.AddDays(-1) || today == expected.AddDays(1))
            .ShouldBeTrue();
    }

    [Fact]
    public void Today_EqualsDateOnlyOfUtcNowAtSameInstant()
    {
        var sut = new DateTimeProvider();

        var utcNow = sut.UtcNow;
        var today = sut.Today;

        var derived = DateOnly.FromDateTime(utcNow);
        (today == derived || today == derived.AddDays(1)).ShouldBeTrue();
    }
}
