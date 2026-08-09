using Domain.Inventory.Aggregates;

namespace Tests.TestInfrastructure.Builders;

public sealed class WarehouseBuilder
{
    private static readonly Faker Faker = new("en");

    private string _code = "WH-" + Faker.Random.AlphaNumeric(4).ToUpperInvariant();
    private string _name = Faker.Company.CompanyName();
    private string _city = Faker.Address.City();
    private string? _address = Faker.Address.StreetAddress();
    private string? _phone = Faker.Phone.PhoneNumber("021########");
    private int _priority = Faker.Random.Int(1, 100);
    private bool _isDefault;

    public WarehouseBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public WarehouseBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public WarehouseBuilder WithCity(string city)
    {
        _city = city;
        return this;
    }

    public WarehouseBuilder WithAddress(string? address)
    {
        _address = address;
        return this;
    }

    public WarehouseBuilder WithPhone(string? phone)
    {
        _phone = phone;
        return this;
    }

    public WarehouseBuilder WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    public WarehouseBuilder AsDefault()
    {
        _isDefault = true;
        return this;
    }

    public Warehouse Build() =>
        Warehouse.Create(_code, _name, _city, _address, _phone, _priority, _isDefault);
}
