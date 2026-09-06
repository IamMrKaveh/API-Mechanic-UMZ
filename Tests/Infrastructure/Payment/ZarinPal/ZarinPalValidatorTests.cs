using System.Reflection;

namespace Tests.Infrastructure.Payment.ZarinPal;

public class ZarinPalValidatorTests
{
    private static MethodInfo ValidateMethod()
    {
        var type = typeof(DBContext).Assembly.GetType(
            "Infrastructure.Payment.ZarinPal.ZarinPalValidator");
        type.ShouldNotBeNull();
        var method = type!.GetMethod("ValidateRequest", BindingFlags.Public | BindingFlags.Static);
        method.ShouldNotBeNull();
        return method!;
    }

    private static void Validate(decimal amount, string description, string callbackUrl)
    {
        try
        {
            ValidateMethod().Invoke(null, [amount, description, callbackUrl]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static string ValidateAndGetMessage(decimal amount, string description, string callbackUrl)
    {
        var ex = Should.Throw<ArgumentException>(() => Validate(amount, description, callbackUrl));
        return ex.Message;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(999)]
    [InlineData(999.99)]
    [InlineData(-1000)]
    public void ValidateRequest_AmountBelowMinimum_Throws(decimal amount)
    {
        ValidateAndGetMessage(amount, "valid description", "https://shop.example.com/callback")
            .ShouldBe("مبلغ تراکنش باید حداقل 1000 ریال باشد.");
    }

    [Fact]
    public void ValidateRequest_AmountExactlyMinimum_Passes()
    {
        Should.NotThrow(() => Validate(1000, "valid description", "https://shop.example.com/callback"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRequest_EmptyDescription_Throws(string? description)
    {
        ValidateAndGetMessage(1000, description!, "https://shop.example.com/callback")
            .ShouldBe("توضیحات تراکنش الزامی است.");
    }

    [Fact]
    public void ValidateRequest_DescriptionLongerThan500Chars_Throws()
    {
        var description = new string('ا', 501);

        ValidateAndGetMessage(1000, description, "https://shop.example.com/callback")
            .ShouldBe("توضیحات تراکنش نباید بیشتر از 500 کاراکتر باشد.");
    }

    [Fact]
    public void ValidateRequest_DescriptionExactly500Chars_Passes()
    {
        Should.NotThrow(() => Validate(1000, new string('ا', 500), "https://shop.example.com/callback"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    [InlineData("ftp://")]
    public void ValidateRequest_InvalidCallbackUrl_Throws(string? callbackUrl)
    {
        ValidateAndGetMessage(1000, "valid description", callbackUrl!)
            .ShouldBe("آدرس بازگشت نامعتبر است.");
    }

    [Theory]
    [InlineData("https://shop.example.com/callback")]
    [InlineData("http://localhost:5000/callback")]
    [InlineData("https://shop.example.com/callback?order=1")]
    public void ValidateRequest_ValidCallbackUrl_Passes(string callbackUrl)
    {
        Should.NotThrow(() => Validate(150_000, "خرید تستی", callbackUrl));
    }

    [Fact]
    public void ValidateRequest_FullyValidInput_DoesNotThrow()
    {
        Should.NotThrow(() => Validate(150_000, "پرداخت سفارش", "https://shop.example.com/payment/callback"));
    }

    [Fact]
    public void ValidateRequest_AmountCheckedBeforeOtherRules()
    {
        // amount invalid + description invalid + callback invalid => amount error wins
        ValidateAndGetMessage(10, "", "bad-url")
            .ShouldBe("مبلغ تراکنش باید حداقل 1000 ریال باشد.");
    }

    [Fact]
    public void ValidateRequest_DescriptionCheckedBeforeCallbackUrl()
    {
        ValidateAndGetMessage(1000, "", "bad-url")
            .ShouldBe("توضیحات تراکنش الزامی است.");
    }
}
