using Domain.Payment.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Payment.ValueObjects;

public class PaymentMethodFeeTests
{
    [Fact]
    public void None_ReturnsFeeThatIsZero()
    {
        var sut = PaymentMethodFee.None();

        sut.Amount.Amount.ShouldBe(0m);
        sut.Percentage.ShouldBe(0m);
        sut.IsZero.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithValidValues_ReturnsFee()
    {
        var sut = PaymentMethodFee.Create(500m, 2.5m);

        sut.Amount.Amount.ShouldBe(500m);
        sut.Percentage.ShouldBe(2.5m);
        sut.IsZero.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithNegativeFixedAmount_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => PaymentMethodFee.Create(-1m, 0m));
    }

    [Fact]
    public void Create_WithNegativePercentage_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => PaymentMethodFee.Create(0m, -0.01m));
    }

    [Fact]
    public void Create_WithPercentageOver100_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => PaymentMethodFee.Create(0m, 100.01m));
    }

    [Fact]
    public void Create_WithPercentageExactly100_ReturnsFee()
    {
        PaymentMethodFee.Create(0m, 100m).Percentage.ShouldBe(100m);
    }

    [Fact]
    public void CalculateFor_WithNoneFee_ReturnsZero()
    {
        PaymentMethodFee.None().CalculateFor(Money.Create(1000m)).Amount.ShouldBe(0m);
    }

    [Fact]
    public void CalculateFor_WithFixedAmountOnly_ReturnsFixedAmount()
    {
        var fee = PaymentMethodFee.Create(500m, 0m);

        fee.CalculateFor(Money.Create(10_000m)).Amount.ShouldBe(500m);
    }

    [Fact]
    public void CalculateFor_WithPercentageOnly_ReturnsRoundedPercentagePart()
    {
        var fee = PaymentMethodFee.Create(0m, 2m);

        fee.CalculateFor(Money.Create(10_000m)).Amount.ShouldBe(200m);
    }

    [Fact]
    public void CalculateFor_WithFixedPlusPercentage_ReturnsSum()
    {
        var fee = PaymentMethodFee.Create(500m, 1m);

        fee.CalculateFor(Money.Create(10_000m)).Amount.ShouldBe(600m);
    }

    [Fact]
    public void CalculateFor_RoundsPercentagePartToZeroDecimals()
    {
        var fee = PaymentMethodFee.Create(0m, 1m);

        fee.CalculateFor(Money.Create(1_234m)).Amount.ShouldBe(12m);
    }

    [Fact]
    public void CalculateFor_WithNullOrderTotal_ReturnsZero()
    {
        PaymentMethodFee.Create(500m, 5m).CalculateFor(null!).Amount.ShouldBe(0m);
    }

    [Fact]
    public void Equality_ForSameAmountAndPercentage_TreatsInstancesAsEqual()
    {
        PaymentMethodFee.Create(500m, 2.5m).ShouldBe(PaymentMethodFee.Create(500m, 2.5m));
    }

    [Fact]
    public void Equality_ForDifferentPercentage_TreatsInstancesAsUnequal()
    {
        PaymentMethodFee.Create(500m, 2.5m).ShouldNotBe(PaymentMethodFee.Create(500m, 3m));
    }
}
