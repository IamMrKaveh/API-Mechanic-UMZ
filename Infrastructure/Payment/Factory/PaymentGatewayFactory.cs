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

        if (_zarinPalOptions.UseSandbox && IsZarinPalFamily(requested))
            requested = "ZarinpalSandbox";

        var gateway = _gateways.FirstOrDefault(g =>
            g.GatewayName.Equals(requested, StringComparison.OrdinalIgnoreCase));

        if (gateway is null)
            throw new InvalidOperationException($"درگاه پرداخت '{requested}' یافت نشد.");

        return gateway;
    }

    public IReadOnlyList<string> GetAvailableGateways()
        => _gateways.Select(g => g.GatewayName).ToList().AsReadOnly();

    private string GetDefaultGatewayName()
        => _zarinPalOptions.UseSandbox ? "ZarinpalSandbox" : "Zarinpal";

    private static bool IsZarinPalFamily(string name)
        => name.Equals("Zarinpal", StringComparison.OrdinalIgnoreCase)
        || name.Equals("ZarinpalSandbox", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeGatewayName(string name)
    {
        if (name.Equals("zarinpal", StringComparison.OrdinalIgnoreCase)) return "Zarinpal";
        if (name.Equals("zarinpalsandbox", StringComparison.OrdinalIgnoreCase)) return "ZarinpalSandbox";
        return name;
    }
}