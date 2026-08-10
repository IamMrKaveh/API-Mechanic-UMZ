using Domain.Inventory.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Domain.Inventory.ValueObjects;

public class StockQuantityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(1_000_000)]
    public void Create_WithNonNegativeValueWithinMax_ReturnsInstance(int value)
    {
        var sut = StockQuantity.Create(value);

        sut.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithNegativeValue_ThrowsDomainException(int value)
    {
        var ex = Should.Throw<DomainException>(() => StockQuantity.Create(value));

        ex.Message.ShouldBe("موجودی نمی‌تواند منفی باشد.");
    }

    [Fact]
    public void Create_WithValueAboveMax_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(() => StockQuantity.Create(1_000_001));

        ex.Message.ShouldContain("1,000,000");
    }

    [Fact]
    public void Add_WithNonNegativeQuantity_ReturnsIncreasedValue()
    {
        var sut = StockQuantity.Create(10);

        sut.Add(5).Value.ShouldBe(15);
    }

    [Fact]
    public void Add_ReturnsNewInstance_NotMutateOriginal()
    {
        var sut = StockQuantity.Create(10);

        var result = sut.Add(5);

        sut.Value.ShouldBe(10);
        result.ShouldNotBeSameAs(sut);
    }

    [Fact]
    public void Add_WithNegativeQuantity_ThrowsDomainException()
    {
        var sut = StockQuantity.Create(10);

        var ex = Should.Throw<DomainException>(() => sut.Add(-1));

        ex.Message.ShouldBe("مقدار افزایش نمی‌تواند منفی باشد.");
    }

    [Fact]
    public void Add_WhenResultExceedsMax_ThrowsDomainException()
    {
        var sut = StockQuantity.Create(1_000_000);

        Should.Throw<DomainException>(() => sut.Add(1));
    }

    [Fact]
    public void Subtract_WithValidQuantity_ReturnsDecreasedValue()
    {
        var sut = StockQuantity.Create(10);

        sut.Subtract(3).Value.ShouldBe(7);
    }

    [Fact]
    public void Subtract_WithNegativeQuantity_ThrowsDomainException()
    {
        var sut = StockQuantity.Create(10);

        var ex = Should.Throw<DomainException>(() => sut.Subtract(-1));

        ex.Message.ShouldBe("مقدار کاهش نمی‌تواند منفی باشد.");
    }

    [Fact]
    public void Subtract_WhenInsufficient_ThrowsDomainException()
    {
        var sut = StockQuantity.Create(5);

        var ex = Should.Throw<DomainException>(() => sut.Subtract(10));

        ex.Message.ShouldContain("موجودی کافی نیست");
        ex.Message.ShouldContain("5");
        ex.Message.ShouldContain("10");
    }

    [Fact]
    public void TrySubtract_WithValidQuantity_ReturnsSuccessWithNewValue()
    {
        var sut = StockQuantity.Create(10);

        var result = sut.TrySubtract(3);

        result.ShouldBeSuccess();
        result.Value.Value.ShouldBe(7);
    }

    [Fact]
    public void TrySubtract_WithNegativeQuantity_ReturnsValidationFailure()
    {
        var sut = StockQuantity.Create(10);

        var result = sut.TrySubtract(-1);

        result.ShouldFailWith("400");
        result.ShouldFailWithType(ErrorType.Validation);
        result.Error.Message.ShouldBe("مقدار کاهش نمی‌تواند منفی باشد.");
    }

    [Fact]
    public void TrySubtract_WhenInsufficient_ReturnsValidationFailureWithShortageMessage()
    {
        var sut = StockQuantity.Create(3);

        var result = sut.TrySubtract(10);

        result.ShouldFailWith("400");
        result.ShouldFailWithType(ErrorType.Validation);
        result.Error.Message.ShouldContain("موجودی کافی نیست");
    }

    [Fact]
    public void ImplicitOperator_ToInt_ReturnsUnderlyingValue()
    {
        var sut = StockQuantity.Create(42);

        int extracted = sut;

        extracted.ShouldBe(42);
    }

    [Theory]
    [InlineData(10, 5, true, false, true, false)]
    [InlineData(5, 10, false, true, false, true)]
    [InlineData(7, 7, false, false, true, true)]
    public void ComparisonOperators_ProduceExpectedResults(
        int left, int right,
        bool expectedGt, bool expectedLt,
        bool expectedGte, bool expectedLte)
    {
        var l = StockQuantity.Create(left);
        var r = StockQuantity.Create(right);

        (l > r).ShouldBe(expectedGt);
        (l < r).ShouldBe(expectedLt);
        (l >= r).ShouldBe(expectedGte);
        (l <= r).ShouldBe(expectedLte);
    }

    [Fact]
    public void CompareTo_WithNull_ReturnsPositive()
    {
        var sut = StockQuantity.Create(10);

        sut.CompareTo(null).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_WithSmallerOther_ReturnsPositive()
    {
        StockQuantity.Create(10).CompareTo(StockQuantity.Create(5)).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_WithEqualOther_ReturnsZero()
    {
        StockQuantity.Create(10).CompareTo(StockQuantity.Create(10)).ShouldBe(0);
    }

    [Fact]
    public void ToString_FormatsWithThousandsSeparator()
    {
        StockQuantity.Create(123456).ToString().ShouldBe("123,456");
    }

    [Fact]
    public void Equality_TwoInstancesWithSameValue_TreatedAsEqual()
    {
        StockQuantity.Create(50).ShouldBe(StockQuantity.Create(50));
    }

    [Fact]
    public void Equality_TwoInstancesWithDifferentValue_TreatedAsUnequal()
    {
        StockQuantity.Create(50).ShouldNotBe(StockQuantity.Create(51));
    }
}
