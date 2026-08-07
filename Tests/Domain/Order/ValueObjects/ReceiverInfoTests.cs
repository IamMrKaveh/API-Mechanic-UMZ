using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Order.ValueObjects;

public class ReceiverInfoTests
{
    [Fact]
    public void Create_WithValidFullNameAndPhone_ReturnsReceiverInfoWithTrimmedFullNameAndDigitOnlyPhone()
    {
        var sut = ReceiverInfo.Create("  Ali Rezaei  ", "09121234567");

        sut.FullName.ShouldBe("Ali Rezaei");
        sut.PhoneNumber.ShouldBe("09121234567");
    }

    [Fact]
    public void Create_StripsNonDigitCharactersFromPhone()
    {
        ReceiverInfo.Create("Ali", "0912-123-4567").PhoneNumber.ShouldBe("09121234567");
    }

    [Fact]
    public void Create_WithParenthesesAndSpacesInPhone_StripsThem()
    {
        ReceiverInfo.Create("Ali", "(912) 123 4567").PhoneNumber.ShouldBe("9121234567");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceFullName_ThrowsDomainException(string? fullName)
    {
        Should.Throw<DomainException>(() => ReceiverInfo.Create(fullName!, "09121234567"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespacePhone_ThrowsDomainException(string? phone)
    {
        Should.Throw<DomainException>(() => ReceiverInfo.Create("Ali", phone!));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abc")]
    public void Create_WithPhoneShorterThan10DigitsAfterNormalization_ThrowsDomainException(string phone)
    {
        Should.Throw<DomainException>(() => ReceiverInfo.Create("Ali", phone));
    }

    [Fact]
    public void Create_WithPhoneLongerThan15DigitsAfterNormalization_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => ReceiverInfo.Create("Ali", "1234567890123456"));
    }

    [Fact]
    public void ToString_ConcatenatesFullNameAndPhone()
    {
        var sut = ReceiverInfo.Create("Ali Rezaei", "09121234567");

        sut.ToString().ShouldBe("Ali Rezaei (09121234567)");
    }

    [Fact]
    public void Equality_ForRecordWithSameFields_TreatsInstancesAsEqual()
    {
        ReceiverInfo.Create("Ali", "09121234567").ShouldBe(ReceiverInfo.Create("Ali", "09121234567"));
    }
}
