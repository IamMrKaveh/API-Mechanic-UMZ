namespace Infrastructure.Payment.Factory;

/// <summary>
/// پیاده‌سازی Factory برای مدیریت چند درگاه پرداخت.
/// درگاه‌های جدید فقط با ثبت در DI Container اضافه می‌شوند.
/// </summary>
public sealed class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly PaymentGatewayOptions _options;
    private readonly ILogger<PaymentGatewayFactory> _logger;

    public PaymentGatewayFactory(
        IEnumerable<IPaymentGateway> gateways,
        IOptions<PaymentGatewayOptions> options,
        ILogger<PaymentGatewayFactory> logger)
    {
        _gateways = gateways;
        _options = options.Value;
        _logger = logger;
    }

    public IPaymentGateway GetGateway(string gatewayName)
    {
        var gateway = _gateways.FirstOrDefault(g =>
            g.GatewayName.Equals(gatewayName, StringComparison.OrdinalIgnoreCase));

        if (gateway is null)
        {
            _logger.LogError("Payment gateway '{Gateway}' not found.", gatewayName);
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
        return _gateways.Select(g => g.GatewayName);
    }
}