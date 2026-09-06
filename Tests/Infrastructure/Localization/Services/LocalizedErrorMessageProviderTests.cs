using System.Globalization;
using Infrastructure.Localization.Services;

namespace Tests.Infrastructure.Localization.Services;

public class LocalizedErrorMessageProviderTests
{
    private readonly LocalizedErrorMessageProvider _sut = new();

    private static void WithUiCulture(string name, Action action)
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(name);
            action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetMessage_BlankCode_ReturnsEmpty(string? code)
    {
        _sut.GetMessage(code!).ShouldBe(string.Empty);
    }

    [Fact]
    public void GetMessage_UnknownCode_ReturnsCodeItself()
    {
        WithUiCulture("fa-IR", () =>
            _sut.GetMessage("error.unknown.code").ShouldBe("error.unknown.code"));
    }

    [Fact]
    public void GetMessage_FarsiCulture_ReturnsPersianMessage()
    {
        WithUiCulture("fa-IR", () =>
            _sut.GetMessage("error.order.not_found").ShouldBe("سفارش یافت نشد."));
    }

    [Fact]
    public void GetMessage_EnglishCulture_ReturnsEnglishMessage()
    {
        WithUiCulture("en-US", () =>
            _sut.GetMessage("error.order.not_found").ShouldBe("Order not found."));
    }

    [Fact]
    public void GetMessage_WithArgumentsAndNoPlaceholders_ReturnsTemplate()
    {
        WithUiCulture("en-US", () =>
            _sut.GetMessage("error.order.not_found", 1, "x").ShouldBe("Order not found."));
    }

    [Fact]
    public void GetMessage_WithNoArguments_ReturnsTemplate()
    {
        WithUiCulture("fa-IR", () =>
            _sut.GetMessage("error.order.not_found", []).ShouldBe("سفارش یافت نشد."));
    }

    [Fact]
    public void GetMessage_WithUnformattableTemplate_ReturnsTemplate()
    {
        WithUiCulture("fa-IR", () =>
            _sut.GetMessage("template { broken", "arg").ShouldBe("template { broken"));
    }

    [Fact]
    public void TryGetMessage_BlankCode_ReturnsFalseWithEmptyMessage()
    {
        _sut.TryGetMessage("  ", out var message).ShouldBeFalse();
        message.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryGetMessage_KnownCode_ReturnsTrueWithMessage()
    {
        WithUiCulture("en-US", () =>
        {
            _sut.TryGetMessage("error.wallet.insufficient_balance", out var message).ShouldBeTrue();
            message.ShouldBe("Wallet has insufficient balance.");
        });
    }

    [Fact]
    public void TryGetMessage_UnknownCode_ReturnsFalseWithEmptyMessage()
    {
        WithUiCulture("fa-IR", () =>
        {
            _sut.TryGetMessage("error.nope", out var message).ShouldBeFalse();
            message.ShouldBe(string.Empty);
        });
    }
}
