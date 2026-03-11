namespace Tests.DomainTest.ValueObjects;

public class PercentageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(0.5)]
    public void Create_WithValidValue_ShouldSucceed(decimal value)
    {
        var percentage = Percentage.Create(value);

        percentage.Should().NotBeNull();
        percentage.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithNegativeValue_ShouldThrowDomainException()
    {
        var act = () => Percentage.Create(-1m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithValueAbove100_ShouldThrowDomainException()
    {
        var act = () => Percentage.Create(101m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Zero_ShouldHaveZeroValue()
    {
        var zero = Percentage.Zero;

        zero.Value.Should().Be(0);
    }

    [Fact]
    public void Full_ShouldHave100Value()
    {
        var full = Percentage.Full;

        full.Value.Should().Be(100);
    }

    [Fact]
    public void ApplyTo_ShouldCalculatePercentageOfAmount()
    {
        var percentage = Percentage.Create(10m);

        var result = percentage.Apply(500_000m);

        result.Should().Be(50_000m);
    }

    [Fact]
    public void TwoPercentages_WithSameValue_ShouldBeEqual()
    {
        var a = Percentage.Create(20m);
        var b = Percentage.Create(20m);

        a.Should().Be(b);
    }

    [Fact]
    public void TwoPercentages_WithDifferentValues_ShouldNotBeEqual()
    {
        var a = Percentage.Create(10m);
        var b = Percentage.Create(20m);

        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedPercentage()
    {
        var percentage = Percentage.Create(15m);

        percentage.ToString().Should().Be("15%");
    }
}