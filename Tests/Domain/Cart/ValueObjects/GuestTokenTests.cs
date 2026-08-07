using Domain.Cart.ValueObjects;
using SharedKernel.Abstractions;

namespace Tests.Domain.Cart.ValueObjects;

public class GuestTokenTests
{
    [Fact]
    public void Generate_ProducesUppercaseHexStringOfLength32()
    {
        var sut = GuestToken.Generate();

        sut.Value.Length.ShouldBe(32);
        sut.Value.ShouldMatch("^[0-9A-F]{32}$");
    }

    [Fact]
    public void Generate_TwoInvocationsProduceDifferentTokens()
    {
        GuestToken.Generate().Value.ShouldNotBe(GuestToken.Generate().Value);
    }

    [Fact]
    public void Create_WithValidLongValue_ReturnsTokenWithTrimmedValue()
    {
        var sut = GuestToken.Create("  ABCD1234EFGH  ");

        sut.Value.ShouldBe("ABCD1234EFGH");
    }

    [Fact]
    public void Create_AtExactlyMinimumLength_Succeeds()
    {
        GuestToken.Create("12345678").Value.ShouldBe("12345678");
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("abc")]
    [InlineData("       12       ")]
    public void Create_WithLengthBelow8AfterTrim_ThrowsArgumentException(string input)
    {
        Should.Throw<ArgumentException>(() => GuestToken.Create(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsArgumentException(string? input)
    {
        Should.Throw<ArgumentException>(() => GuestToken.Create(input!));
    }

    [Fact]
    public void TryCreate_WithValidValue_ReturnsToken()
    {
        var sut = GuestToken.TryCreate("ABCD1234EFGH");

        sut.ShouldNotBeNull();
        sut!.Value.ShouldBe("ABCD1234EFGH");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    public void TryCreate_WithInvalidValue_ReturnsNull(string? input)
    {
        GuestToken.TryCreate(input).ShouldBeNull();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        GuestToken.Create("ABCD1234EFGH").ToString().ShouldBe("ABCD1234EFGH");
    }

    [Fact]
    public void Equality_ForSameValue_TreatsInstancesAsEqual()
    {
        GuestToken.Create("ABCD1234EFGH").ShouldBe(GuestToken.Create("ABCD1234EFGH"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        GuestToken.Create("ABCD1234EFGH").ShouldNotBe(GuestToken.Create("WXYZ5678IJKL"));
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        GuestToken.Generate().ShouldBeAssignableTo<ValueObject>();
    }
}
