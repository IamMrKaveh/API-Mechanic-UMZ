using Infrastructure.Security.Services;

namespace Tests.Infrastructure.Security.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_ForValidPassword_ReturnsBcryptFormattedHash()
    {
        var hash = _sut.Hash("Str0ngP@ssw0rd!");

        hash.ShouldNotBeNullOrWhiteSpace();
        hash.StartsWith("$2").ShouldBeTrue();
    }

    [Fact]
    public void Hash_CalledTwiceForSamePassword_ReturnsDifferentHashesDueToSalt()
    {
        const string password = "Str0ngP@ssw0rd!";

        var first = _sut.Hash(password);
        var second = _sut.Hash(password);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void Hash_ForNullPassword_ThrowsArgumentException()
    {
        Action act = () => _sut.Hash(null!);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Hash_ForEmptyOrWhitespacePassword_ThrowsArgumentException(string password)
    {
        Action act = () => _sut.Hash(password);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Verify_ForCorrectPasswordAgainstHash_ReturnsTrue()
    {
        const string password = "Str0ngP@ssw0rd!";
        var hash = _sut.Hash(password);

        var isValid = _sut.Verify(password, hash);

        isValid.ShouldBeTrue();
    }

    [Fact]
    public void Verify_ForIncorrectPasswordAgainstHash_ReturnsFalse()
    {
        var hash = _sut.Hash("Str0ngP@ssw0rd!");

        var isValid = _sut.Verify("wrong-password", hash);

        isValid.ShouldBeFalse();
    }

    [Fact]
    public void Verify_IsCaseSensitive()
    {
        var hash = _sut.Hash("Str0ngP@ssw0rd!");

        var lowered = _sut.Verify("str0ngp@ssw0rd!", hash);
        var uppered = _sut.Verify("STR0NGP@SSW0RD!", hash);

        lowered.ShouldBeFalse();
        uppered.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Verify_ForNullOrWhitespacePassword_ReturnsFalseWithoutThrowing(string? password)
    {
        var hash = _sut.Hash("Str0ngP@ssw0rd!");

        var isValid = _sut.Verify(password!, hash);

        isValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Verify_ForNullOrWhitespaceHash_ReturnsFalseWithoutThrowing(string? hash)
    {
        var isValid = _sut.Verify("Str0ngP@ssw0rd!", hash!);

        isValid.ShouldBeFalse();
    }

    [Fact]
    public void Verify_ForMalformedHash_ReturnsFalse()
    {
        var isValid = _sut.Verify("Str0ngP@ssw0rd!", "not-a-bcrypt-hash");

        isValid.ShouldBeFalse();
    }
}
