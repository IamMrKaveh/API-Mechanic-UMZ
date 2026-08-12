using Application.Common.Exceptions;

namespace Tests.Application.Common.Exceptions;

public class ConcurrencyExceptionTests
{
    [Fact]
    public void Constructor_WithoutMessage_UsesPersianDefault()
    {
        var sut = new ConcurrencyException();

        sut.Message.ShouldBe("تغییرات همزمان رخ داده است. لطفاً دوباره تلاش کنید.");
    }

    [Fact]
    public void Constructor_WithCustomMessage_UsesProvidedMessage()
    {
        var sut = new ConcurrencyException("row was updated by another user");

        sut.Message.ShouldBe("row was updated by another user");
    }

    [Fact]
    public void Type_IsAssignableToException()
    {
        var sut = new ConcurrencyException();

        sut.ShouldBeAssignableTo<Exception>();
    }
}
