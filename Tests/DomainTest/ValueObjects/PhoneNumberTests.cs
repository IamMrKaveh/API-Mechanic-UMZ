namespace Tests.DomainTest.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("09123456789")]
    [InlineData("09001234567")]
    [InlineData("09901234567")]
    public void Create_WithValidIranMobileNumber_ShouldSucceed(string phone)
    {
        var phoneNumber = PhoneNumber.Create(phone);

        phoneNumber.Should().NotBeNull();
        phoneNumber.Value.Should().Be(phone);
    }

    [Fact]
    public void Create_WithNullValue_ShouldThrowDomainException()
    {
        var act = () => PhoneNumber.Create(null!);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithEmptyString_ShouldThrowDomainException()
    {
        var act = () => PhoneNumber.Create("");

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("0212345678")]
    [InlineData("abcdefghijk")]
    public void Create_WithInvalidFormat_ShouldThrowException(string phone)
    {
        var act = () => PhoneNumber.Create(phone);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_WithInternationalFormat_ShouldNormalize()
    {
        var phoneNumber = PhoneNumber.Create("+989123456789");

        phoneNumber.Value.Should().Be("09123456789");
    }

    [Fact]
    public void GetMasked_ShouldMaskMiddleDigits()
    {
        var phone = PhoneNumber.Create("09123456789");

        var masked = phone.GetMasked();

        masked.Should().StartWith("0912");
        masked.Should().EndWith("6789");
        masked.Should().Contain("***");
    }

    [Fact]
    public void GetInternationalFormat_ShouldReturnWithPlusSuffix()
    {
        var phone = PhoneNumber.Create("09123456789");

        var international = phone.GetInternationalFormat();

        international.Should().Be("+989123456789");
    }

    [Fact]
    public void Matches_WithSameNumber_ShouldReturnTrue()
    {
        var phone = PhoneNumber.Create("09123456789");

        phone.Matches("09123456789").Should().BeTrue();
    }

    [Fact]
    public void Matches_WithDifferentNumber_ShouldReturnFalse()
    {
        var phone = PhoneNumber.Create("09123456789");

        phone.Matches("09987654321").Should().BeFalse();
    }

    [Fact]
    public void TwoPhoneNumbers_WithSameValue_ShouldBeEqual()
    {
        var a = PhoneNumber.Create("09123456789");
        var b = PhoneNumber.Create("09123456789");

        a.Should().Be(b);
    }

    [Fact]
    public void TwoPhoneNumbers_WithDifferentValues_ShouldNotBeEqual()
    {
        var a = PhoneNumber.Create("09123456789");
        var b = PhoneNumber.Create("09987654321");

        a.Should().NotBe(b);
    }

    [Fact]
    public void TryCreate_WithValidNumber_ShouldReturnSuccess()
    {
        var (success, phone, error) = PhoneNumber.TryCreate("09123456789");

        success.Should().BeTrue();
        phone.Should().NotBeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryCreate_WithInvalidNumber_ShouldReturnFailure()
    {
        var (success, phone, error) = PhoneNumber.TryCreate("invalid");

        success.Should().BeFalse();
        phone.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
    }
}