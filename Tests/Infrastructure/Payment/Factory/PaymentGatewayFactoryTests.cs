using Application.Payment.Contracts;
using Infrastructure.Payment.Factory;
using Infrastructure.Payment.ZarinPal.Options;
using Microsoft.Extensions.Options;

namespace Tests.Infrastructure.Payment.Factory;

public class PaymentGatewayFactoryTests
{
    private static IPaymentGateway Gateway(string name)
    {
        var g = Substitute.For<IPaymentGateway>(); g.GatewayName.Returns(name); return g;
    }

    private static IOptions<ZarinPalOptions> Options(bool useSandbox)
        => Microsoft.Extensions.Options.Options.Create(new ZarinPalOptions { UseSandbox = useSandbox });

    [Fact]
    public void GetGateway_EmptyNameWithSandboxEnabled_ReturnsZarinpalSandbox()
    {
        var sandbox = Gateway("ZarinpalSandbox");
        var live = Gateway("Zarinpal");
        var sut = new PaymentGatewayFactory(new[] { live, sandbox }, Options(useSandbox: true));

        var result = sut.GetGateway("");

        result.GatewayName.ShouldBe("ZarinpalSandbox");
    }

    [Fact]
    public void GetGateway_EmptyNameWithSandboxDisabled_ReturnsZarinpal()
    {
        var sandbox = Gateway("ZarinpalSandbox");
        var live = Gateway("Zarinpal");
        var sut = new PaymentGatewayFactory(new[] { live, sandbox }, Options(useSandbox: false));

        var result = sut.GetGateway("");

        result.GatewayName.ShouldBe("Zarinpal");
    }

    [Fact]
    public void GetGateway_ZarinpalRequestedWithSandboxEnabled_RedirectsToSandbox()
    {
        var sandbox = Gateway("ZarinpalSandbox");
        var live = Gateway("Zarinpal");
        var sut = new PaymentGatewayFactory(new[] { live, sandbox }, Options(useSandbox: true));

        var result = sut.GetGateway("Zarinpal");

        result.GatewayName.ShouldBe("ZarinpalSandbox");
    }

    [Fact]
    public void GetGateway_ZarinpalRequestedWithSandboxDisabled_ReturnsZarinpal()
    {
        var sandbox = Gateway("ZarinpalSandbox");
        var live = Gateway("Zarinpal");
        var sut = new PaymentGatewayFactory(new[] { live, sandbox }, Options(useSandbox: false));

        var result = sut.GetGateway("Zarinpal");

        result.GatewayName.ShouldBe("Zarinpal");
    }

    [Theory]
    [InlineData("zarin-pal")]
    [InlineData("zarin_pal")]
    [InlineData("ZARINPAL")]
    public void GetGateway_NormalizedZarinpalVariants_ReturnZarinpal(string requested)
    {
        var live = Gateway("Zarinpal");
        var sut = new PaymentGatewayFactory(new[] { live }, Options(useSandbox: false));

        var result = sut.GetGateway(requested);

        result.GatewayName.ShouldBe("Zarinpal");
    }

    [Fact]
    public void GetGateway_MatchingCustomGatewayName_ReturnsThatGateway()
    {
        var mock = Gateway("MockGateway");
        var live = Gateway("Zarinpal");
        var sut = new PaymentGatewayFactory(new[] { live, mock }, Options(useSandbox: false));

        var result = sut.GetGateway("MockGateway");

        result.GatewayName.ShouldBe("MockGateway");
    }

    [Fact]
    public void GetGateway_UnknownGatewayName_ThrowsInvalidOperationException()
    {
        var live = Gateway("Zarinpal");
        var sut = new PaymentGatewayFactory(new[] { live }, Options(useSandbox: false));

        Should.Throw<InvalidOperationException>(() => sut.GetGateway("no-such-gateway"));
    }

    [Fact]
    public void GetAvailableGateways_ReturnsNamesOfAllRegisteredGateways()
    {
        var live = Gateway("Zarinpal");
        var sandbox = Gateway("ZarinpalSandbox");
        var mock = Gateway("MockGateway");
        var sut = new PaymentGatewayFactory(new[] { live, sandbox, mock }, Options(useSandbox: false));

        var names = sut.GetAvailableGateways();

        names.Count.ShouldBe(3);
        names.ShouldContain("Zarinpal");
        names.ShouldContain("ZarinpalSandbox");
        names.ShouldContain("MockGateway");
    }
}
