using Domain.Payment.Aggregates;

namespace Tests.Builders.Payment;

public class PaymentTransactionBuilder
{
    private int _orderId = 1;
    private int _userId = 1;
    private string _authority = "AUTH-TEST-001";
    private decimal _amount = 500_000m;
    private string _gateway = "Zarinpal";
    private string? _description = null;
    private string? _ipAddress = null;
    private int _expiryMinutes = 20;

    public PaymentTransactionBuilder WithOrderId(int orderId)
    {
        _orderId = orderId;
        return this;
    }

    public PaymentTransactionBuilder WithUserId(int userId)
    {
        _userId = userId;
        return this;
    }

    public PaymentTransactionBuilder WithAuthority(string authority)
    {
        _authority = authority;
        return this;
    }

    public PaymentTransactionBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public PaymentTransactionBuilder WithGateway(string gateway)
    {
        _gateway = gateway;
        return this;
    }

    public PaymentTransactionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public PaymentTransactionBuilder WithIpAddress(string ip)
    {
        _ipAddress = ip;
        return this;
    }

    public PaymentTransaction Build()
    {
        return PaymentTransaction.Initiate(
            _orderId,
            _userId,
            _authority,
            _amount,
            _gateway,
            _description,
            _ipAddress,
            expiryMinutes: _expiryMinutes);
    }

    public PaymentTransaction BuildProcessing()
    {
        var tx = Build();
        tx.MarkAsVerificationInProgress();
        return tx;
    }

    public PaymentTransaction BuildSucceeded(long refId = 123456789L)
    {
        var tx = BuildProcessing();
        tx.MarkAsSuccess(refId);
        return tx;
    }

    public PaymentTransaction BuildFailed()
    {
        var tx = BuildProcessing();
        tx.MarkAsFailed("خطای تست");
        return tx;
    }
}