using SharedKernel.Guard;
using SharedGuard = SharedKernel.Guard.Guard;

namespace Tests.SharedKernel.Guard;

public class GuardTests
{
    [Fact]
    public void AgainstNull_WithNull_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            SharedGuard.Against.Null<string>(null!, "name"));

        ex.ParamName.ShouldBe("name");
    }

    [Fact]
    public void AgainstNull_WithValue_DoesNotThrow()
    {
        Should.NotThrow(() => SharedGuard.Against.Null("value", "name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrWhiteSpace_WithBlank_ThrowsArgumentException(string? value)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            SharedGuard.Against.NullOrWhiteSpace(value!, "name"));

        ex.ParamName.ShouldBe("name");
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_WithValue_DoesNotThrow()
    {
        Should.NotThrow(() => SharedGuard.Against.NullOrWhiteSpace("ok", "name"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void AgainstNegativeOrZeroInt_WithNonPositive_Throws(int value)
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() =>
            SharedGuard.Against.NegativeOrZero(value, "qty"));

        ex.ParamName.ShouldBe("qty");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void AgainstNegativeOrZeroInt_WithPositive_DoesNotThrow(int value)
    {
        Should.NotThrow(() => SharedGuard.Against.NegativeOrZero(value, "qty"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void AgainstNegativeOrZeroDecimal_WithNonPositive_Throws(decimal value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            SharedGuard.Against.NegativeOrZero(value, "amount"));
    }

    [Fact]
    public void AgainstNegativeOrZeroDecimal_WithPositive_DoesNotThrow()
    {
        Should.NotThrow(() => SharedGuard.Against.NegativeOrZero(0.01m, "amount"));
    }

    [Fact]
    public void AgainstNegativeInt_WithNegative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            SharedGuard.Against.Negative(-5, "stock"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void AgainstNegativeInt_WithNonNegative_DoesNotThrow(int value)
    {
        Should.NotThrow(() => SharedGuard.Against.Negative(value, "stock"));
    }

    [Fact]
    public void AgainstEmpty_WithNull_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            SharedGuard.Against.Empty<string>(null!, "items"));
    }

    [Fact]
    public void AgainstEmpty_WithEmptyCollection_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            SharedGuard.Against.Empty(Array.Empty<string>(), "items"));

        ex.ParamName.ShouldBe("items");
    }

    [Fact]
    public void AgainstEmpty_WithItems_DoesNotThrow()
    {
        Should.NotThrow(() => SharedGuard.Against.Empty(new[] { "a" }, "items"));
    }
}
