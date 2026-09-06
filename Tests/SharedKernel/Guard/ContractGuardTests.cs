using SharedKernel.Guard;

namespace Tests.SharedKernel.Guard;

public class ContractGuardTests
{
    [Fact]
    public void AgainstNull_WithNull_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            ContractGuard.Against.Null<string>(null!, "dep"));

        ex.ParamName.ShouldBe("dep");
    }

    [Fact]
    public void AgainstNull_WithValue_DoesNotThrow()
    {
        Should.NotThrow(() => ContractGuard.Against.Null("v", "dep"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AgainstNullOrWhiteSpace_WithBlank_Throws(string? value)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            ContractGuard.Against.NullOrWhiteSpace(value!, "p"));

        ex.ParamName.ShouldBe("p");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AgainstNegativeOrZeroInt_WithNonPositive_Throws(int value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ContractGuard.Against.NegativeOrZero(value, "p"));
    }

    [Fact]
    public void AgainstNegativeOrZeroInt_WithPositive_DoesNotThrow()
    {
        Should.NotThrow(() => ContractGuard.Against.NegativeOrZero(1, "p"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void AgainstNegativeOrZeroDecimal_WithNonPositive_Throws(decimal value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ContractGuard.Against.NegativeOrZero(value, "p"));
    }

    [Fact]
    public void AgainstNegativeOrZeroDecimal_WithPositive_DoesNotThrow()
    {
        Should.NotThrow(() => ContractGuard.Against.NegativeOrZero(2.5m, "p"));
    }

    [Fact]
    public void AgainstNegativeInt_WithNegative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ContractGuard.Against.Negative(-1, "p"));
    }

    [Fact]
    public void AgainstNegativeDecimal_WithNegative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ContractGuard.Against.Negative(-0.1m, "p"));
    }

    [Fact]
    public void AgainstNegative_WithZero_DoesNotThrow()
    {
        Should.NotThrow(() => ContractGuard.Against.Negative(0, "p"));
        Should.NotThrow(() => ContractGuard.Against.Negative(0m, "p"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new int[0])]
    public void AgainstEmpty_WithNullOrEmpty_Throws(int[]? value)
    {
        Should.Throw<ArgumentException>(() =>
            ContractGuard.Against.Empty(value!, "p"));
    }

    [Fact]
    public void AgainstOutOfRange_BelowMinOrAboveMax_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ContractGuard.Against.OutOfRange(-1, 0, 10, "p"));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ContractGuard.Against.OutOfRange(11, 0, 10, "p"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void AgainstOutOfRange_WithinBounds_DoesNotThrow(int value)
    {
        Should.NotThrow(() => ContractGuard.Against.OutOfRange(value, 0, 10, "p"));
    }

    [Fact]
    public void AgainstLengthExceeds_LongerThanMax_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            ContractGuard.Against.LengthExceeds("abcdef", 5, "p"));

        ex.ParamName.ShouldBe("p");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("abcde")]
    public void AgainstLengthExceeds_NullOrWithinMax_DoesNotThrow(string? value)
    {
        Should.NotThrow(() => ContractGuard.Against.LengthExceeds(value!, 5, "p"));
    }

    [Fact]
    public void AgainstFalse_WithFalseCondition_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            ContractGuard.Against.False(false, "p", "must hold"));

        ex.ParamName.ShouldBe("p");
        ex.Message.ShouldContain("must hold");
    }

    [Fact]
    public void AgainstFalse_WithTrueCondition_DoesNotThrow()
    {
        Should.NotThrow(() => ContractGuard.Against.False(true, "p", "must hold"));
    }

    [Fact]
    public void AgainstTrue_WithTrueCondition_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            ContractGuard.Against.True(true, "p", "must not hold"));
    }

    [Fact]
    public void AgainstTrue_WithFalseCondition_DoesNotThrow()
    {
        Should.NotThrow(() => ContractGuard.Against.True(false, "p", "must not hold"));
    }
}
