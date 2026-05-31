using Application.Payment.Contracts;

namespace Infrastructure.Payment.Factory;

public sealed class PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways) : IPaymentGatewayFactory
{
    public IPaymentGateway GetGateway(string gatewayName = "zarinpal")
    {
        var gateway = gateways.FirstOrDefault(g =>
            g.GatewayName.Equals(gatewayName, StringComparison.OrdinalIgnoreCase));

        return gateway is null
            ? throw new InvalidOperationException(
                $"درگاه پرداخت '{gatewayName}' پیدا نشد. درگاه‌های موجود: {string.Join(", ", GetAvailableGateways())}")
            : gateway;
    }

    public IReadOnlyList<string> GetAvailableGateways()
        => gateways.Select(g => g.GatewayName).ToList().AsReadOnly();
}