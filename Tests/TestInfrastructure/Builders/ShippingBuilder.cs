using Bogus;
using Domain.Shipping.Aggregates;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class ShippingBuilder
{
    private static readonly Faker Faker = new("en");

    private ShippingName _name = ShippingName.Create(Faker.Commerce.Product() + " Ship");
    private Money _baseCost = Money.FromDecimal(Faker.Random.Decimal(10_000m, 500_000m));
    private string? _description = Faker.Lorem.Sentence(5);
    private string? _estimatedDeliveryTime = null;
    private int _minDeliveryDays = 1;
    private int _maxDeliveryDays = 7;

    private bool _asDefault;
    private bool _asDeleted;
    private UserId? _deletedBy;
    private bool _clearEventsAfterBuild;

    public ShippingBuilder WithName(ShippingName name)
    {
        _name = name;
        return this;
    }

    public ShippingBuilder WithName(string name)
    {
        _name = ShippingName.Create(name);
        return this;
    }

    public ShippingBuilder WithBaseCost(Money baseCost)
    {
        _baseCost = baseCost;
        return this;
    }

    public ShippingBuilder WithBaseCost(decimal amount, string currency = "IRT")
    {
        _baseCost = Money.FromDecimal(amount, currency);
        return this;
    }

    public ShippingBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public ShippingBuilder WithEstimatedDeliveryTime(string? label)
    {
        _estimatedDeliveryTime = label;
        return this;
    }

    public ShippingBuilder WithDeliveryDays(int min, int max)
    {
        _minDeliveryDays = min;
        _maxDeliveryDays = max;
        return this;
    }

    public ShippingBuilder AsDefault()
    {
        _asDefault = true;
        return this;
    }

    public ShippingBuilder AsDeleted(UserId? deletedBy = null)
    {
        _asDeleted = true;
        _deletedBy = deletedBy;
        return this;
    }

    public ShippingBuilder ClearEventsAfterBuild()
    {
        _clearEventsAfterBuild = true;
        return this;
    }

    public Shipping Build()
    {
        var shipping = Shipping.Create(
            _name,
            _baseCost,
            _description,
            _estimatedDeliveryTime,
            _minDeliveryDays,
            _maxDeliveryDays);

        if (_asDefault)
            shipping.SetAsDefault();

        if (_asDeleted)
        {
            if (shipping.IsDefault)
                shipping.UnsetDefault();
            shipping.RequestDeletion(_deletedBy);
        }

        if (_clearEventsAfterBuild)
            shipping.ClearDomainEvents();

        return shipping;
    }
}

