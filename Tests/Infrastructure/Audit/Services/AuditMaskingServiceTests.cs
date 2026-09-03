using Domain.User.ValueObjects;
using Infrastructure.Audit.Services;
using SharedKernel.ValueObjects;

namespace Tests.Infrastructure.Audit.Services;

public class AuditMaskingServiceTests
{
    private readonly AuditMaskingService _sut = new();

    [Theory]
    [InlineData("09121234567", "091****4567")]
    [InlineData("09120000000", "091****0000")]
    public void MaskPhoneNumber_WithValidNumber_MasksMiddleDigits(string input, string expected)
    {
        var result = _sut.MaskPhoneNumber(PhoneNumber.Create(input));

        result.ShouldBe(expected);
    }

    [Fact]
    public void MaskPhoneNumber_WithNull_ReturnsEmpty()
    {
        var result = _sut.MaskPhoneNumber(null!);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void MaskSensitiveData_WithShortDigitString_LeavesItUntouched()
    {
        const string input = "Call 123456 now";

        var result = _sut.MaskSensitiveData(input);

        result.ShouldBe(input);
    }

    [Theory]
    [InlineData("ali.rezaei@example.com")]
    [InlineData("a@example.com")]
    [InlineData("ab@example.com")]
    public void MaskEmail_NeverExposesFullUsername(string input)
    {
        var result = _sut.MaskEmail(Email.Create(input));

        result.ShouldContain("@example.com");
        result.ShouldNotBe(input);
    }

    [Fact]
    public void MaskEmail_WithNull_ReturnsEmpty()
    {
        var result = _sut.MaskEmail(null!);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void MaskEmail_WithLongUsername_ShowsFirstAndLastLetterOnly()
    {
        var result = _sut.MaskEmail(Email.Create("john.doe@example.com"));

        result.ShouldBe("j*****e@example.com");
    }

    [Theory]
    [InlineData("127.0.0.1", "127.0.*.*")]
    [InlineData("192.168.1.100", "192.168.*.*")]
    public void MaskIpAddress_WithIPv4_MasksLastTwoOctets(string input, string expected)
    {
        var result = _sut.MaskIpAddress(IpAddress.Create(input));

        result.ShouldBe(expected);
    }

    [Fact]
    public void MaskIpAddress_WithNull_ReturnsEmpty()
    {
        var result = _sut.MaskIpAddress(null!);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void MaskSensitiveData_WithBearerToken_MasksTokenKeepingScheme()
    {
        const string input = "Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.payload.sig";

        var result = _sut.MaskSensitiveData(input);

        result.ShouldContain("Bearer [MASKED-TOKEN]");
        result.ShouldNotContain("eyJhbGciOiJIUzI1NiJ9");
    }

    [Fact]
    public void MaskSensitiveData_WithCardNumber_MasksMiddleGroups()
    {
        const string input = "card 6037991122334455 charged";

        var result = _sut.MaskSensitiveData(input);

        result.ShouldContain("6037-****-****-4455");
        result.ShouldNotContain("6037991122334455");
    }

    [Fact]
    public void MaskSensitiveData_WithPhoneNumber_MasksIt()
    {
        const string input = "user phone 09121234567 called";

        var result = _sut.MaskSensitiveData(input);

        result.ShouldNotContain("09121234567");
        result.ShouldContain("0912-***-4567");
    }

    [Fact]
    public void MaskSensitiveData_WithEmail_MasksUsername()
    {
        const string input = "contact john.doe@example.com please";

        var result = _sut.MaskSensitiveData(input);

        result.ShouldNotContain("john.doe@example.com");
        result.ShouldContain("@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MaskSensitiveData_WithNullOrEmpty_ReturnsInput(string? input)
    {
        var result = _sut.MaskSensitiveData(input!);

        result.ShouldBe(input);
    }

    [Fact]
    public void MaskSensitiveData_WithPlainText_ReturnsUnchanged()
    {
        const string input = "Order 12345 was shipped successfully";

        var result = _sut.MaskSensitiveData(input);

        result.ShouldBe(input);
    }
}
