using Domain.Security.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace Tests.Domain.Security.ValueObjects;

public class RefreshTokenTests
{
    [Fact]
    public void Create_AtExactlyMinLength_Succeeds()
    {
        var input = new string('a', 32);

        RefreshToken.Create(input).Value.Length.ShouldBe(32);
    }

    [Fact]
    public void Create_AtExactlyMaxLength_Succeeds()
    {
        var input = new string('a', 512);

        RefreshToken.Create(input).Value.Length.ShouldBe(512);
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsBeforeValidating()
    {
        var payload = new string('a', 40);

        RefreshToken.Create($"  {payload}  ").Value.ShouldBe(payload);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        Should.Throw<DomainException>(() => RefreshToken.Create(input!));
    }

    [Fact]
    public void Create_BelowMinLengthAfterTrimming_ThrowsDomainException()
    {
        var input = new string('a', 31);

        Should.Throw<DomainException>(() => RefreshToken.Create(input));
    }

    [Fact]
    public void Create_OneAboveMaxLength_ThrowsDomainException()
    {
        var input = new string('a', 513);

        Should.Throw<DomainException>(() => RefreshToken.Create(input));
    }

    [Fact]
    public void Generate_ProducesTokenWithLengthWithinAllowedRange()
    {
        var token = RefreshToken.Generate();

        token.Value.Length.ShouldBeGreaterThanOrEqualTo(32);
        token.Value.Length.ShouldBeLessThanOrEqualTo(512);
    }

    [Fact]
    public void Generate_TwoInvocations_ProduceDifferentTokens()
    {
        RefreshToken.Generate().Value.ShouldNotBe(RefreshToken.Generate().Value);
    }

    [Fact]
    public void Generate_ProducesTokenAcceptedByCreate()
    {
        var generated = RefreshToken.Generate();

        Should.NotThrow(() => RefreshToken.Create(generated.Value));
    }

    [Fact]
    public void Matches_WithIdenticalRawValue_ReturnsTrue()
    {
        var payload = new string('x', 64);

        RefreshToken.Create(payload).Matches(payload).ShouldBeTrue();
    }

    [Fact]
    public void Matches_WithSurroundingWhitespace_ReturnsTrueAfterTrim()
    {
        var payload = new string('x', 64);

        RefreshToken.Create(payload).Matches($"  {payload}  ").ShouldBeTrue();
    }

    [Fact]
    public void Matches_WithDifferentValue_ReturnsFalse()
    {
        RefreshToken.Create(new string('x', 64)).Matches(new string('y', 64)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Matches_WithNullOrWhitespaceOther_ReturnsFalse(string? other)
    {
        RefreshToken.Create(new string('x', 64)).Matches(other!).ShouldBeFalse();
    }

    [Fact]
    public void Matches_IsCaseSensitive()
    {
        var lower = new string('a', 64);
        var upper = new string('A', 64);

        RefreshToken.Create(lower).Matches(upper).ShouldBeFalse();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var payload = new string('t', 40);

        RefreshToken.Create(payload).ToString().ShouldBe(payload);
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var payload = new string('t', 40);

        string s = RefreshToken.Create(payload);

        s.ShouldBe(payload);
    }

    [Fact]
    public void Equality_ForSameValue_TreatsInstancesAsEqual()
    {
        var payload = new string('t', 40);

        RefreshToken.Create(payload).ShouldBe(RefreshToken.Create(payload));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        RefreshToken.Create(new string('a', 40)).ShouldNotBe(RefreshToken.Create(new string('b', 40)));
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        RefreshToken.Create(new string('t', 40)).ShouldBeAssignableTo<ValueObject>();
    }
}
