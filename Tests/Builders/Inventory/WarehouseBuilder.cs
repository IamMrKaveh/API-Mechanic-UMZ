namespace Tests.Builders.Inventory;

public class WarehouseBuilder
{
    private string _code = "WH-001";
    private string _name = "انبار مرکزی";
    private string _city = "تهران";
    private string? _address = "خیابان آزادی";
    private string? _phone = "021-12345678";
    private int _priority = 0;
    private bool _isDefault = false;

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

    public WarehouseBuilder AsDefault()
    {
        _isDefault = true;
        return this;
    }

    public WarehouseBuilder WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    public Warehouse Build()
    {
        return Warehouse.Create(_code, _name, _city, _address, _phone, _priority, _isDefault);
    }
}