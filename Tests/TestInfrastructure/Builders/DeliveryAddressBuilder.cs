using Domain.Order.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class DeliveryAddressBuilder
{
    private string _province = "تهران";
    private string _city = "تهران";
    private string _street = "خیابان ولیعصر، پلاک ۱۲۳";
    private string _postalCode = "1234567890";

    public DeliveryAddressBuilder WithProvince(string value)
    {
        _province = value;
        return this;
    }

    public DeliveryAddressBuilder WithCity(string value)
    {
        _city = value;
        return this;
    }

    public DeliveryAddressBuilder WithStreet(string value)
    {
        _street = value;
        return this;
    }

    public DeliveryAddressBuilder WithPostalCode(string value)
    {
        _postalCode = value;
        return this;
    }

    public DeliveryAddress Build() => DeliveryAddress.Create(_province, _city, _street, _postalCode);
}
