using SharedKernel.Exceptions;
using SharedKernel.Guard;

namespace Tests.SharedKernel.Guard;

public class DomainGuardTests
{
    [Fact]
    public void AgainstNull_WithNull_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(() =>
            DomainGuard.Against.Null<string>(null!, "شیء الزامی است."));

        ex.Message.ShouldBe("شیء الزامی است.");
        ex.ErrorCode.ShouldBe("DOMAIN_ERROR");
    }

    [Fact]
    public void AgainstNull_WithValue_DoesNotThrow()
    {
        Should.NotThrow(() => DomainGuard.Against.Null("v", "msg"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AgainstNullOrWhiteSpace_WithBlank_ThrowsDomainException(string? value)
    {
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.NullOrWhiteSpace(value!, "متن الزامی است."));
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_WithValue_DoesNotThrow()
    {
        Should.NotThrow(() => DomainGuard.Against.NullOrWhiteSpace("ok", "msg"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void AgainstNegativeOrZeroInt_WithNonPositive_Throws(int value)
    {
        var ex = Should.Throw<DomainException>(() =>
            DomainGuard.Against.NegativeOrZero(value, "باید مثبت باشد."));

        ex.Message.ShouldBe("باید مثبت باشد.");
    }

    [Fact]
    public void AgainstNegativeOrZeroInt_WithPositive_DoesNotThrow()
    {
        Should.NotThrow(() => DomainGuard.Against.NegativeOrZero(3, "msg"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.25)]
    public void AgainstNegativeOrZeroDecimal_WithNonPositive_Throws(decimal value)
    {
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.NegativeOrZero(value, "msg"));
    }

    [Fact]
    public void AgainstNegativeOrZeroDecimal_WithPositive_DoesNotThrow()
    {
        Should.NotThrow(() => DomainGuard.Against.NegativeOrZero(1.5m, "msg"));
    }

    [Fact]
    public void AgainstNegativeInt_WithNegative_Throws()
    {
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.Negative(-1, "منفی ممنوع."));
    }

    [Fact]
    public void AgainstNegativeDecimal_WithNegative_Throws()
    {
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.Negative(-0.5m, "منفی ممنوع."));
    }

    [Fact]
    public void AgainstNegative_WithZero_DoesNotThrow()
    {
        Should.NotThrow(() => DomainGuard.Against.Negative(0, "msg"));
        Should.NotThrow(() => DomainGuard.Against.Negative(0m, "msg"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new int[0])]
    public void AgainstEmpty_WithNullOrEmpty_Throws(int[]? value)
    {
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.Empty(value!, "خالی است."));
    }

    [Fact]
    public void AgainstEmpty_WithItems_DoesNotThrow()
    {
        Should.NotThrow(() => DomainGuard.Against.Empty(new[] { 1 }, "msg"));
    }

    [Fact]
    public void AgainstOutOfRangeInt_OutsideBounds_Throws()
    {
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.OutOfRange(-1, 0, 10, "خارج از بازه."));
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.OutOfRange(11, 0, 10, "خارج از بازه."));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void AgainstOutOfRangeInt_OnBoundaries_DoesNotThrow(int value)
    {
        Should.NotThrow(() => DomainGuard.Against.OutOfRange(value, 0, 10, "msg"));
    }

    [Fact]
    public void AgainstOutOfRangeDecimal_OutsideBounds_Throws()
    {
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.OutOfRange(10.5m, 0m, 10m, "خارج از بازه."));
    }

    [Fact]
    public void AgainstOutOfRangeDecimal_InsideBounds_DoesNotThrow()
    {
        Should.NotThrow(() => DomainGuard.Against.OutOfRange(5.5m, 0m, 10m, "msg"));
    }

    [Fact]
    public void AgainstLengthExceeds_LongerThanMax_Throws()
    {
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.LengthExceeds("abcdef", 5, "طولانی است."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abcde")]
    public void AgainstLengthExceeds_NullOrWithinMax_DoesNotThrow(string? value)
    {
        Should.NotThrow(() => DomainGuard.Against.LengthExceeds(value!, 5, "msg"));
    }

    [Fact]
    public void AgainstFalse_WithFalseCondition_Throws()
    {
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.False(false, "شرط برقرار نیست."));
    }

    [Fact]
    public void AgainstFalse_WithTrueCondition_DoesNotThrow()
    {
        Should.NotThrow(() => DomainGuard.Against.False(true, "msg"));
    }

    [Fact]
    public void AgainstTrue_WithTrueCondition_Throws()
    {
        Should.Throw<DomainException>(() =>
            DomainGuard.Against.True(true, "نباید برقرار باشد."));
    }

    [Fact]
    public void AgainstTrue_WithFalseCondition_DoesNotThrow()
    {
        Should.NotThrow(() => DomainGuard.Against.True(false, "msg"));
    }
}
