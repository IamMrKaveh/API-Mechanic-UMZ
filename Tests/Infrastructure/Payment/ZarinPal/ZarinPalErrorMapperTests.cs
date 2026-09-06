using System.Reflection;

namespace Tests.Infrastructure.Payment.ZarinPal;

public class ZarinPalErrorMapperTests
{
    private static string GetMessage(int code)
    {
        var type = typeof(DBContext).Assembly.GetType(
            "Infrastructure.Payment.ZarinPal.ZarinPalErrorMapper");
        type.ShouldNotBeNull();
        var method = type!.GetMethod("GetMessage", BindingFlags.Public | BindingFlags.Static);
        method.ShouldNotBeNull();
        return (string)method!.Invoke(null, [code])!;
    }

    [Theory]
    [InlineData(100, "عملیات موفقیت‌آمیز بود.")]
    [InlineData(101, "تراکنش قبلاً تایید شده است.")]
    [InlineData(-9, "اطلاعات ارسال شده ناقص است.")]
    [InlineData(-10, "آی‌پی درگاه با آی‌پی ثبت شده مغایرت دارد.")]
    [InlineData(-11, "مرچنت کد نامعتبر است.")]
    [InlineData(-12, "تلاش بیش از حد در بازه زمانی کوتاه.")]
    [InlineData(-22, "شناسه پرداخت نامعتبر یا منقضی شده است.")]
    [InlineData(-50, "مبلغ پرداخت معتبر نیست.")]
    [InlineData(-51, "پرداخت یافت نشد.")]
    [InlineData(-52, "خطای غیرمنتظره در درگاه.")]
    [InlineData(-53, "شناسه پرداخت با تراکنش مطابقت ندارد.")]
    [InlineData(-54, "درخواست مورد نظر آرشیو شده است.")]
    [InlineData(-1, "اطلاعات ارسال شده ناقص است.")]
    public void GetMessage_KnownCode_ReturnsExpectedMessage(int code, string expected)
    {
        GetMessage(code).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(102)]
    [InlineData(-2)]
    [InlineData(-100)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void GetMessage_UnknownCode_ReturnsFallbackMessage(int code)
    {
        GetMessage(code).ShouldBe("خطای ناشناخته در درگاه پرداخت.");
    }

    [Fact]
    public void GetMessage_AllEnumValues_HaveExplicitMapping()
    {
        var enumType = typeof(DBContext).Assembly.GetType(
            "Infrastructure.Payment.ZarinPal.ZarinPalErrorCode");
        enumType.ShouldNotBeNull();

        foreach (var value in Enum.GetValues(enumType!).Cast<object>().Select(Convert.ToInt32))
            GetMessage(value).ShouldNotBe("خطای ناشناخته در درگاه پرداخت.");
    }

    [Fact]
    public void GetMessage_ReturnsNonEmptyMessage_ForEveryMappedCode()
    {
        foreach (var code in new[] { 100, 101, -9, -10, -11, -12, -22, -50, -51, -52, -53, -54, -1 })
            GetMessage(code).ShouldNotBeNullOrWhiteSpace();
    }
}
