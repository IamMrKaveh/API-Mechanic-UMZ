using Application.Payment.Contracts;
using Infrastructure.Payment.ZarinPal.Options;

namespace Infrastructure.Payment.Factory;

public sealed class PaymentGatewayFactory(
    IEnumerable<IPaymentGateway> gateways,
    IOptions<ZarinPalOptions> zarinPalOptions) : IPaymentGatewayFactory
{
    private readonly ZarinPalOptions _zarinPalOptions = zarinPalOptions.Value;
    private readonly IReadOnlyList<IPaymentGateway> _gateways = gateways.ToList().AsReadOnly();

    public IPaymentGateway GetGateway(string gatewayName = "")
    {
        var requested = string.IsNullOrWhiteSpace(gatewayName)
            ? GetDefaultGatewayName()
            : NormalizeGatewayName(gatewayName);

        if (_zarinPalOptions.IsSandbox)
            requested = "ZarinpalSandbox";

        var gateway = _gateways.FirstOrDefault(g =>
            g.GatewayName.Equals(requested, StringComparison.OrdinalIgnoreCase));

        return gateway is null
            ? throw new InvalidOperationException(
                $"درگاه پرداخت '{requested}' پیدا نشد. درگاه‌های موجود: {string.Join(", ", GetAvailableGateways())}")
            : gateway;
    }

    public IReadOnlyList<string> GetAvailableGateways()
        => _gateways.Select(g => g.GatewayName).ToList().AsReadOnly();

    private string GetDefaultGatewayName()
        => _zarinPalOptions.IsSandbox ? "ZarinpalSandbox" : "Zarinpal";

    private static string NormalizeGatewayName(string name)
    {
        if (name.Equals("zarinpal", StringComparison.OrdinalIgnoreCase)) return "Zarinpal";
        if (name.Equals("zarinpalsandbox", StringComparison.OrdinalIgnoreCase)) return "ZarinpalSandbox";
        return name;
    }
}