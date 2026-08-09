using Domain.Shipping.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Shipping.ValueObjects;

public class DeliveryTimeRangeTests
{
    [Fact]
    public void Create_WithValidRange_StoresMinAndMax()
    {
        var sut = DeliveryTimeRange.Create(2, 5);

        sut.MinDays.ShouldBe(2);
        sut.MaxDays.ShouldBe(5);
    }

    [Fact]
    public void Create_WithMinEqualsMax_MarksAsSameDay()
    {
        var sut = DeliveryTimeRange.Create(3, 3);

        sut.IsSameDay.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithMinDifferentFromMax_IsNotSameDay()
    {
        DeliveryTimeRange.Create(1, 2).IsSameDay.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithZeroMin_IsAllowed()
    {
        Should.NotThrow(() => DeliveryTimeRange.Create(0, 1));
    }

    [Fact]
    public void Create_WithNegativeMin_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => DeliveryTimeRange.Create(-1, 5));
    }

    [Fact]
    public void Create_WithMaxLessThanMin_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => DeliveryTimeRange.Create(5, 4));
    }

    [Theory]
    [InlineData(0, 366)]
    [InlineData(1, 1000)]
    public void Create_WithMaxAboveAbsoluteLimit_ThrowsDomainException(int min, int max)
    {
        Should.Throw<DomainException>(() => DeliveryTimeRange.Create(min, max));
    }

    [Fact]
    public void Create_WithMaxEqualToAbsoluteLimit_IsAllowed()
    {
        Should.NotThrow(() => DeliveryTimeRange.Create(0, 365));
    }

    [Fact]
    public void ToDisplayString_ForSameDay_ReturnsSingleDayFormat()
    {
        DeliveryTimeRange.Create(3, 3).ToDisplayString().ShouldBe("3 روز کاری");
    }

    [Fact]
    public void ToDisplayString_ForRange_ReturnsMinToMaxFormat()
    {
        DeliveryTimeRange.Create(2, 5).ToDisplayString().ShouldBe("2 تا 5 روز کاری");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToDisplayString_WithNullOrEmptyCustomLabel_FallsBackToDefaultFormat(string? label)
    {
        DeliveryTimeRange.Create(2, 5).ToDisplayString(label).ShouldBe("2 تا 5 روز کاری");
    }

    [Fact]
    public void ToDisplayString_WithCustomLabel_PrefersCustomLabel()
    {
        DeliveryTimeRange.Create(2, 5).ToDisplayString("۲۴ ساعته").ShouldBe("۲۴ ساعته");
    }

    [Fact]
    public void Equality_ForSameMinAndMax_TreatsInstancesAsEqual()
    {
        DeliveryTimeRange.Create(2, 5).ShouldBe(DeliveryTimeRange.Create(2, 5));
    }

    [Fact]
    public void Equality_ForDifferentMinOrMax_TreatsInstancesAsNotEqual()
    {
        DeliveryTimeRange.Create(2, 5).ShouldNotBe(DeliveryTimeRange.Create(2, 6));
    }

    [Fact]
    public void ToString_ReturnsSameOutputAsToDisplayString()
    {
        var sut = DeliveryTimeRange.Create(2, 5);

        sut.ToString().ShouldBe(sut.ToDisplayString());
    }
}

