using Infrastructure.Common.Options;
using Microsoft.Extensions.Configuration;

namespace Tests.Infrastructure.Common.Options;

public class FrontendUrlsOptionsTests
{
    [Fact]
    public void SectionName_HasExpectedValue()
    {
        FrontendUrlsOptions.SectionName.ShouldBe("FrontendUrls");
    }

    [Fact]
    public void DefaultConstruction_UsesExpectedDefaultValues()
    {
        var sut = new FrontendUrlsOptions();

        sut.BaseUrl.ShouldBe("http://localhost:4200");
        sut.LocalHostUrl.ShouldBe("http://localhost:4200");
        sut.PaymentSuccessPath.ShouldBe("/payment/success");
        sut.PaymentFailurePath.ShouldBe("/payment/failure");
        sut.WalletTopUpCallbackPath.ShouldBe("/dashboard/wallet/topup/callback");
    }

    [Fact]
    public void ConfigurationBinding_WithAllValuesProvided_PopulatesEveryProperty()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontendUrls:BaseUrl"] = "https://shop.example.com",
                ["FrontendUrls:LocalHostUrl"] = "https://local.example.com",
                ["FrontendUrls:PaymentSuccessPath"] = "/checkout/ok",
                ["FrontendUrls:PaymentFailurePath"] = "/checkout/fail",
                ["FrontendUrls:WalletTopUpCallbackPath"] = "/wallet/callback"
            })
            .Build();

        var sut = new FrontendUrlsOptions();
        configuration.GetSection(FrontendUrlsOptions.SectionName).Bind(sut);

        sut.BaseUrl.ShouldBe("https://shop.example.com");
        sut.LocalHostUrl.ShouldBe("https://local.example.com");
        sut.PaymentSuccessPath.ShouldBe("/checkout/ok");
        sut.PaymentFailurePath.ShouldBe("/checkout/fail");
        sut.WalletTopUpCallbackPath.ShouldBe("/wallet/callback");
    }

    [Fact]
    public void ConfigurationBinding_WithPartialValues_LeavesUnspecifiedPropertiesAtDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontendUrls:BaseUrl"] = "https://only-base.example.com"
            })
            .Build();

        var sut = new FrontendUrlsOptions();
        configuration.GetSection(FrontendUrlsOptions.SectionName).Bind(sut);

        sut.BaseUrl.ShouldBe("https://only-base.example.com");
        sut.LocalHostUrl.ShouldBe("http://localhost:4200");
        sut.PaymentSuccessPath.ShouldBe("/payment/success");
        sut.PaymentFailurePath.ShouldBe("/payment/failure");
        sut.WalletTopUpCallbackPath.ShouldBe("/dashboard/wallet/topup/callback");
    }
}
