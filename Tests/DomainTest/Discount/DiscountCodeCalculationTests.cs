namespace Tests.DomainTest.Discount;

public class DiscountCodeCalculationTests
{
    [Fact]
    public void CalculateDiscountAmount_WithSimplePercentage_ShouldReturnCorrectAmount()
    {
        var discount = new DiscountCodeBuilder()
            .WithPercentage(10)
            .Build();

        var amount = discount.CalculateDiscountAmount(1000, DateTime.UtcNow);

        amount.Should().Be(100);
    }

    [Fact]
    public void CalculateDiscountAmount_WithMaxDiscountCap_ShouldReturnCappedAmount()
    {
        var discount = new DiscountCodeBuilder()
            .WithPercentage(50)
            .WithMaxDiscountAmount(200)
            .Build();

        var amount = discount.CalculateDiscountAmount(1000, DateTime.UtcNow);

        amount.Should().Be(200);
    }

    [Fact]
    public void CalculateDiscountAmount_WhenCalculatedLessThanCap_ShouldReturnCalculated()
    {
        var discount = new DiscountCodeBuilder()
            .WithPercentage(10)
            .WithMaxDiscountAmount(500)
            .Build();

        var amount = discount.CalculateDiscountAmount(1000, DateTime.UtcNow);

        amount.Should().Be(100);
    }

    [Fact]
    public void CalculateDiscountAmount_ForInactiveDiscount_ShouldReturnZero()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();

        var amount = discount.CalculateDiscountAmount(1000, DateTime.UtcNow);

        amount.Should().Be(0);
    }

    [Fact]
    public void CalculateDiscountAmount_BelowMinOrderAmount_ShouldReturnZero()
    {
        var discount = new DiscountCodeBuilder()
            .WithMinOrderAmount(2000)
            .Build();

        var amount = discount.CalculateDiscountAmount(1000, DateTime.UtcNow);

        amount.Should().Be(0);
    }

    [Fact]
    public void CalculateDiscountAmount_ShouldRoundToZeroDecimals()
    {
        var discount = new DiscountCodeBuilder()
            .WithPercentage(10)
            .Build();

        var amount = discount.CalculateDiscountAmount(155, DateTime.UtcNow);

        amount.Should().Be(16);
    }

    [Fact]
    public void TryApply_WithValidDiscount_ShouldReturnSuccessResult()
    {
        var discount = new DiscountCodeBuilder().Build();

        var result = discount.Apply(1000, DateTime.UtcNow, 1, 0);

        result.IsSuccess.Should().BeTrue();
        result.DiscountAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TryApply_WithInvalidDiscount_ShouldReturnFailureResult()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();

        var result = discount.Apply(1000, DateTime.UtcNow, 1, 0);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetEffectivePercentage_WhenNoCap_ShouldReturnSameAsPercentage()
    {
        var discount = new DiscountCodeBuilder()
            .WithPercentage(20)
            .Build();

        var effective = discount.GetEffectivePercentage(1000, DateTime.UtcNow);

        effective.Should().Be(20);
    }

    [Fact]
    public void GetEffectivePercentage_WhenCapApplied_ShouldReturnLowerPercentage()
    {
        var discount = new DiscountCodeBuilder()
            .WithPercentage(50)
            .WithMaxDiscountAmount(100)
            .Build();

        var effective = discount.GetEffectivePercentage(1000, DateTime.UtcNow);

        effective.Should().Be(10);
    }

    [Fact]
    public void CalculateDiscountMoney_ShouldReturnMoneyValueObject()
    {
        var discount = new DiscountCodeBuilder()
            .WithPercentage(10)
            .Build();

        var money = Money.FromDecimal(1000);
        var result = discount.CalculateDiscountMoney(money, DateTime.UtcNow);

        result.Amount.Should().Be(100);
    }
}