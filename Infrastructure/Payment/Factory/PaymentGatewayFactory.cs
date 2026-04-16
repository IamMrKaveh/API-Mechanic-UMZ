using Application.Payment.Contracts;

namespace Infrastructure.Payment.Factory;

/// <summary>
/// پیاده‌سازی Factory برای مدیریت چند درگاه پرداخت.
/// درگاه‌های جدید فقط با ثبت در DI Container اضافه می‌شوند.
/// </summary>
public sealed class PaymentGatewayFactory(
    IEnumerable<IPaymentGateway> gateways,
    IOptions<PaymentGatewayOptions> options,
    IAuditService auditService) : IPaymentGatewayFactory
{
    private readonly PaymentGatewayOptions _options = options.Value;

    public async Task<IPaymentGateway> GetGateway(string gatewayName)
    {
        var gateway = gateways.FirstOrDefault(g =>
            g.GatewayName.Equals(gatewayName, StringComparison.OrdinalIgnoreCase));

        if (gateway is null)
        {
            await auditService.LogErrorAsync("Payment gateway '{Gateway}' not found.");
            throw new InvalidOperationException(
                $"درگاه پرداخت '{gatewayName}' پیدا نشد. درگاه‌های موجود: {string.Join(", ", GetAvailableGateways())}");
        }

        return gateway;
    }

    public IPaymentGateway GetDefaultGateway()
    {
        return GetGateway(_options.DefaultGateway);
    }

    public IEnumerable<string> GetAvailableGateways()
    {
        return gateways.Select(g => g.GatewayName);
    }
}