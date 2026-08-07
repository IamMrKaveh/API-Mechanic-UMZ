using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class PaymentMethodBuilder
{
    private static readonly Faker Faker = new();

    private PaymentMethodName _name = PaymentMethodName.Create(Faker.Commerce.ProductName().PadRight(3, 'x'));

    private PaymentMethodCode _code = PaymentMethodCode.Create(
        $"pm-{Faker.Random.AlphaNumeric(8).ToLowerInvariant()}");

    private PaymentMethodFee _fee = PaymentMethodFee.None();
    private string? _description = Faker.Lorem.Sentence();
    private string? _iconUrl;
    private int _sortOrder = Faker.Random.Int(0, 100);

    public PaymentMethodBuilder WithName(PaymentMethodName name)
    {
        _name = name;
        return this;
    }

    public PaymentMethodBuilder WithName(string value)
    {
        _name = PaymentMethodName.Create(value);
        return this;
    }

    public PaymentMethodBuilder WithCode(PaymentMethodCode code)
    {
        _code = code;
        return this;
    }

    public PaymentMethodBuilder WithCode(string value)
    {
        _code = PaymentMethodCode.Create(value);
        return this;
    }

    public PaymentMethodBuilder WithFee(PaymentMethodFee fee)
    {
        _fee = fee;
        return this;
    }

    public PaymentMethodBuilder WithFee(decimal fixedAmount, decimal percentage)
    {
        _fee = PaymentMethodFee.Create(fixedAmount, percentage);
        return this;
    }

    public PaymentMethodBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public PaymentMethodBuilder WithIconUrl(string? iconUrl)
    {
        _iconUrl = iconUrl;
        return this;
    }

    public PaymentMethodBuilder WithSortOrder(int sortOrder)
    {
        _sortOrder = sortOrder;
        return this;
    }

    public PaymentMethod Build() =>
        PaymentMethod.Create(_name, _code, _fee, _description, _iconUrl, _sortOrder);
}
