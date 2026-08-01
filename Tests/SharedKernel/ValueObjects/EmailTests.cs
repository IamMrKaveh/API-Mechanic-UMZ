using SharedKernel.ValueObjects;

namespace Tests.SharedKernel.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last@sub.example.co")]
    [InlineData("a@b.c")]
    [InlineData("USER+tag@Example.COM")]
    public void Create_WithValidFormat_ReturnsEmail(string input)
    {
        Should.NotThrow(() => Email.Create(input));
    }

    [Fact]
    public void Create_LowercasesAndTrimsValue()
    {
        var sut = Email.Create("  USER@Example.COM  ");

        sut.Value.ShouldBe("user@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsArgumentException(string input)
    {
        Should.Throw<ArgumentException>(() => Email.Create(input));
    }

    [Fact]
    public void Create_WithNull_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => Email.Create(null!));
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("missing-at-sign.com")]
    [InlineData("user@nodomaindot")]
    [InlineData("user@ example.com")]
    [InlineData("user@@example.com")]
    [InlineData("us er@example.com")]
    public void Create_WithMalformedInput_ThrowsArgumentException(string input)
    {
        Should.Throw<ArgumentException>(() => Email.Create(input));
    }

    [Fact]
    public void CreateTemp_WithPhoneNumber_ProducesTempLocalEmail()
    {
        var sut = Email.CreateTemp("09121234567");

        sut.Value.ShouldBe("09121234567@temp.local");
    }

    [Fact]
    public void CreateTemp_TrimsPhoneNumber()
    {
        var sut = Email.CreateTemp("  09121234567  ");

        sut.Value.ShouldBe("09121234567@temp.local");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateTemp_WithEmptyPhone_ThrowsArgumentException(string phone)
    {
        Should.Throw<ArgumentException>(() => Email.CreateTemp(phone));
    }

    [Fact]
    public void ToString_ReturnsNormalizedValue()
    {
        Email.Create("USER@Example.COM").ToString().ShouldBe("user@example.com");
    }

    [Fact]
    public void Equality_ForValueObjectWithSameNormalizedValue_TreatsInstancesAsEqual()
    {
        Email.Create("USER@Example.com").ShouldBe(Email.Create("user@example.com"));
    }
}
