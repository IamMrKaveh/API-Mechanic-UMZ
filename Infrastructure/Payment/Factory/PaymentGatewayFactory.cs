using Application.Payment.Contracts;
using Microsoft.Extensions.Options;

namespace Infrastructure.Payment.Factory;

public sealed class PaymentGatewayOptions
{
    public string DefaultGateway { get; init; } = "zarinpal";
}

public sealed class PaymentGatewayFactory(
    IEnumerable<IPaymentGateway> gateways,
    IOptions<PaymentGatewayOptions> options) : IPaymentGatewayFactory
{
    private readonly string _defaultGateway = options.Value.DefaultGateway;

    public IPaymentGateway GetGateway(string gatewayName = "zarinpal")
    {
        var gateway = gateways.FirstOrDefault(g =>
            g.GatewayName.Equals(gatewayName, StringComparison.OrdinalIgnoreCase));

        if (gateway is null)
            throw new InvalidOperationException(
                $"درگاه پرداخت '{gatewayName}' پیدا نشد. درگاه‌های موجود: {string.Join(", ", GetAvailableGateways())}");

        return gateway;
    }

    public IReadOnlyList<string> GetAvailableGateways()
        => gateways.Select(g => g.GatewayName).ToList().AsReadOnly();
}