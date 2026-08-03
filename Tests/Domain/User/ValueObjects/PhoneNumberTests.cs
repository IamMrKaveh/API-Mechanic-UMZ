using Domain.User.Exceptions;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;
using SharedKernel.Results;

namespace Tests.Domain.User.ValueObjects;

public class PhoneNumberTests
{
    [Fact]
    public void Create_WithValidIranianMobile_ReturnsPhoneNumber()
    {
        PhoneNumber.Create("09121234567").Value.ShouldBe("09121234567");
    }

    [Theory]
    [InlineData("989121234567", "09121234567")]
    [InlineData("00989121234567", "09121234567")]
    [InlineData("9121234567", "09121234567")]
    public void Create_NormalizesInternationalAndShortFormats(string input, string expected)
    {
        PhoneNumber.Create(input).Value.ShouldBe(expected);
    }

    [Fact]
    public void Create_ConvertsPersianDigitsToAscii()
    {
        PhoneNumber.Create("۰۹۱۲۱۲۳۴۵۶۷").Value.ShouldBe("09121234567");
    }

    [Fact]
    public void Create_StripsNonDigitCharacters()
    {
        PhoneNumber.Create("0912-123-4567").Value.ShouldBe("09121234567");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        Should.Throw<DomainException>(() => PhoneNumber.Create(input!));
    }

    [Theory]
    [InlineData("08121234567")]
    [InlineData("0812")]
    [InlineData("abc")]
    [InlineData("091212345670000")]
    public void Create_WithInvalidFormat_ThrowsInvalidPhoneNumberException(string input)
    {
        Should.Throw<InvalidPhoneNumberException>(() => PhoneNumber.Create(input));
    }

    [Fact]
    public void TryCreate_WithValidInput_ReturnsSuccessResult()
    {
        var result = PhoneNumber.TryCreate("09121234567");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("09121234567");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryCreate_WithNullOrWhitespace_ReturnsFailureWithEmptyCode(string? input)
    {
        var result = PhoneNumber.TryCreate(input!);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("PhoneNumber.Empty");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void TryCreate_WithInvalidFormat_ReturnsFailureWithInvalidFormatCode()
    {
        var result = PhoneNumber.TryCreate("08121234567");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("PhoneNumber.InvalidFormat");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = PhoneNumber.Create("09121234567");

        s.ShouldBe("09121234567");
    }

    [Fact]
    public void Equality_ForSameNormalizedValue_TreatsInstancesAsEqual()
    {
        PhoneNumber.Create("09121234567").ShouldBe(PhoneNumber.Create("989121234567"));
    }

    [Fact]
    public void Equality_ForDifferentNumbers_TreatsInstancesAsUnequal()
    {
        PhoneNumber.Create("09121234567").ShouldNotBe(PhoneNumber.Create("09121234568"));
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        PhoneNumber.Create("09121234567").ShouldBeAssignableTo<ValueObject>();
    }
}
