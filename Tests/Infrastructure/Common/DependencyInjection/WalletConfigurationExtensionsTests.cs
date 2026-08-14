using Application.Wallet.Options;
using Infrastructure.Common.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tests.Infrastructure.Common.DependencyInjection;

public class WalletConfigurationExtensionsTests
{
    [Fact]
    public void AddWalletTransferOptions_ReturnsSameServiceCollectionInstance_ForFluentChaining()
    {
        var services = new ServiceCollection(); var configuration = BuildConfiguration(new Dictionary<string, string?>());

        var result = services.AddWalletTransferOptions(configuration);

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddWalletTransferOptions_WithEmptyConfiguration_ResolvesWalletTransferOptionsWithClassDefaults()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        services.AddWalletTransferOptions(configuration);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<WalletTransferOptions>>().Value;

        options.MinimumAmount.ShouldBe(10000m);
        options.MaximumAmount.ShouldBe(1000000000m);
        options.DailyLimit.ShouldBe(50000000m);
        options.OtpLength.ShouldBe(6);
        options.OtpTtlSeconds.ShouldBe(180);
        options.MaxPendingTransfersPerHour.ShouldBe(5);
        options.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void AddWalletTransferOptions_WithValidConfiguration_BindsProvidedValues()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["WalletTransfer:MinimumAmount"] = "5000",
            ["WalletTransfer:MaximumAmount"] = "5000000",
            ["WalletTransfer:DailyLimit"] = "20000000",
            ["WalletTransfer:OtpLength"] = "5",
            ["WalletTransfer:OtpTtlSeconds"] = "120",
            ["WalletTransfer:MaxPendingTransfersPerHour"] = "10",
            ["WalletTransfer:Currency"] = "USD"
        });
        services.AddWalletTransferOptions(configuration);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<WalletTransferOptions>>().Value;

        options.MinimumAmount.ShouldBe(5000m);
        options.MaximumAmount.ShouldBe(5000000m);
        options.DailyLimit.ShouldBe(20000000m);
        options.OtpLength.ShouldBe(5);
        options.OtpTtlSeconds.ShouldBe(120);
        options.MaxPendingTransfersPerHour.ShouldBe(10);
        options.Currency.ShouldBe("USD");
    }

    [Fact]
    public void AddWalletTransferOptions_WhenMinimumAmountBelowRange_ThrowsOptionsValidationExceptionOnResolve()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["WalletTransfer:MinimumAmount"] = "0"
        });
        services.AddWalletTransferOptions(configuration);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<WalletTransferOptions>>();

        var exception = Should.Throw<OptionsValidationException>(() => _ = options.Value);

        exception.Failures.ShouldContain(
            failure => failure.Contains("MinimumAmount", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddWalletTransferOptions_WhenOtpLengthBelowRange_ThrowsOptionsValidationExceptionOnResolve()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["WalletTransfer:OtpLength"] = "3"
        });
        services.AddWalletTransferOptions(configuration);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<WalletTransferOptions>>();

        Should.Throw<OptionsValidationException>(() => _ = options.Value);
    }

    [Fact]
    public void AddWalletTransferOptions_WhenOtpLengthAboveRange_ThrowsOptionsValidationExceptionOnResolve()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["WalletTransfer:OtpLength"] = "11"
        });
        services.AddWalletTransferOptions(configuration);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<WalletTransferOptions>>();

        Should.Throw<OptionsValidationException>(() => _ = options.Value);
    }

    [Fact]
    public void AddWalletTransferOptions_WhenCurrencyEmpty_ThrowsOptionsValidationExceptionOnResolve()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["WalletTransfer:Currency"] = ""
        });
        services.AddWalletTransferOptions(configuration);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<WalletTransferOptions>>();

        Should.Throw<OptionsValidationException>(() => _ = options.Value);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> data) =>
        new ConfigurationBuilder().AddInMemoryCollection(data).Build();
}
