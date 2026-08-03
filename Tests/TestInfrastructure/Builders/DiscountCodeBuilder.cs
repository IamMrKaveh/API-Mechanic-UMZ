using Domain.Discount.Aggregates;
using Domain.Discount.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class DiscountCodeBuilder
{
    private DiscountCodeId _id = DiscountCodeId.NewId();
    private string _code = "TEST10";
    private DiscountValue _value = DiscountValue.Percentage(10m);
    private Money? _maximumDiscountAmount;
    private int? _usageLimit;
    private DateTime? _startsAt;
    private DateTime? _expiresAt;

    public DiscountCodeBuilder WithId(DiscountCodeId id)
    {
        _id = id;
        return this;
    }

    public DiscountCodeBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public DiscountCodeBuilder WithValue(DiscountValue value)
    {
        _value = value;
        return this;
    }

    public DiscountCodeBuilder WithMaximumDiscountAmount(Money? amount)
    {
        _maximumDiscountAmount = amount;
        return this;
    }

    public DiscountCodeBuilder WithMaximumDiscountAmount(decimal amount, string currency = "IRT")
    {
        _maximumDiscountAmount = Money.Create(amount, currency);
        return this;
    }

    public DiscountCodeBuilder WithUsageLimit(int? limit)
    {
        _usageLimit = limit;
        return this;
    }

    public DiscountCodeBuilder WithStartsAt(DateTime? startsAt)
    {
        _startsAt = startsAt;
        return this;
    }

    public DiscountCodeBuilder WithExpiresAt(DateTime? expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }

    public DiscountCode Build() =>
        DiscountCode.Create(_id, _code, _value, _maximumDiscountAmount, _usageLimit, _startsAt, _expiresAt);
}
