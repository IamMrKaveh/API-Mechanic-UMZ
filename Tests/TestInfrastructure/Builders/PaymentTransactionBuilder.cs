using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class PaymentTransactionBuilder
{
    private static readonly Faker Faker = new();

    private OrderId _orderId = OrderId.NewId();
    private UserId _userId = UserId.NewId();
    private string _authority = $"A{Faker.Random.AlphaNumeric(20)}";
    private decimal _amount = Faker.Random.Decimal(1_000m, 5_000_000m);
    private string _gateway = "Zarinpal";
    private DateTime _now = DateTime.UtcNow;
    private string? _description = Faker.Lorem.Sentence();
    private int _expiryMinutes = 20;

    public PaymentTransactionBuilder WithOrderId(OrderId orderId)
    {
        _orderId = orderId;
        return this;
    }

    public PaymentTransactionBuilder WithUserId(UserId userId)
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

    public PaymentTransactionBuilder WithNow(DateTime now)
    {
        _now = now;
        return this;
    }

    public PaymentTransactionBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public PaymentTransactionBuilder WithExpiryMinutes(int expiryMinutes)
    {
        _expiryMinutes = expiryMinutes;
        return this;
    }

    public PaymentTransaction Build() =>
        PaymentTransaction.Initiate(
            _orderId, _userId, _authority, _amount, _gateway,
            _now, _description, _expiryMinutes);
}
