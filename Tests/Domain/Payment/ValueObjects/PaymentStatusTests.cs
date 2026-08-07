using Domain.Payment.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Payment.ValueObjects;

public class PaymentStatusTests
{
    [Fact]
    public void Pending_HasExpectedValueOrderAndIsFinalFalse()
    {
        var sut = PaymentStatus.Pending;

        sut.Value.ShouldBe("Pending");
        sut.Order.ShouldBe(0);
        sut.IsFinal.ShouldBeFalse();
    }

    [Fact]
    public void Processing_HasExpectedValueOrderAndIsFinalFalse()
    {
        var sut = PaymentStatus.Processing;

        sut.Value.ShouldBe("Processing");
        sut.Order.ShouldBe(1);
        sut.IsFinal.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Success", 2)]
    [InlineData("Failed", 3)]
    [InlineData("Expired", 4)]
    [InlineData("Cancelled", 5)]
    [InlineData("Refunded", 6)]
    public void TerminalStatuses_AreFinal(string valueName, int order)
    {
        var sut = PaymentStatus.FromString(valueName);

        sut.Value.ShouldBe(valueName);
        sut.Order.ShouldBe(order);
        sut.IsFinal.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromString_WithNullOrWhitespace_ReturnsPending(string? value)
    {
        PaymentStatus.FromString(value!).ShouldBe(PaymentStatus.Pending);
    }

    [Theory]
    [InlineData("pending", "Pending")]
    [InlineData("PROCESSING", "Processing")]
    [InlineData("Success", "Success")]
    [InlineData("failed", "Failed")]
    [InlineData("expired", "Expired")]
    [InlineData("cancelled", "Cancelled")]
    [InlineData("refunded", "Refunded")]
    public void FromString_IsCaseInsensitive(string input, string expectedValue)
    {
        PaymentStatus.FromString(input).Value.ShouldBe(expectedValue);
    }

    [Fact]
    public void FromString_WithUnknownValue_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => PaymentStatus.FromString("Approved"));
    }

    [Fact]
    public void Equality_ForSameValue_TreatsInstancesAsEqual()
    {
        PaymentStatus.Success.ShouldBe(PaymentStatus.FromString("success"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        PaymentStatus.Success.ShouldNotBe(PaymentStatus.Failed);
    }

    [Fact]
    public void ToString_ReturnsDisplayName()
    {
        PaymentStatus.Success.ToString().ShouldBe("موفق");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValueName()
    {
        string s = PaymentStatus.Success;

        s.ShouldBe("Success");
    }

    [Fact]
    public void ImplicitConversion_ToInt_ReturnsOrder()
    {
        int order = PaymentStatus.Refunded;

        order.ShouldBe(6);
    }
}
