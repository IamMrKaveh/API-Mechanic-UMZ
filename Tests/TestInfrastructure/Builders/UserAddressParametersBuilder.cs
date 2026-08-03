using Domain.User.Aggregates;
using Domain.User.Entities;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class UserAddressParametersBuilder
{
    private UserAddressId _addressId = UserAddressId.NewId();
    private string _title = "خانه";
    private string _receiverName = "علی رضایی";
    private PhoneNumber _phoneNumber = new PhoneNumberBuilder().Build();
    private string _province = "تهران";
    private string _city = "تهران";
    private string _address = "خیابان ولیعصر، پلاک ۱۲۳";
    private string _postalCode = "1234567890";
    private decimal? _latitude;
    private decimal? _longitude;

    public UserAddressParametersBuilder WithAddressId(UserAddressId id)
    {
        _addressId = id;
        return this;
    }

    public UserAddressParametersBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public UserAddressParametersBuilder WithReceiverName(string name)
    {
        _receiverName = name;
        return this;
    }

    public UserAddressParametersBuilder WithPhoneNumber(PhoneNumber phone)
    {
        _phoneNumber = phone;
        return this;
    }

    public UserAddressParametersBuilder WithProvince(string province)
    {
        _province = province;
        return this;
    }

    public UserAddressParametersBuilder WithCity(string city)
    {
        _city = city;
        return this;
    }

    public UserAddressParametersBuilder WithAddress(string address)
    {
        _address = address;
        return this;
    }

    public UserAddressParametersBuilder WithPostalCode(string postalCode)
    {
        _postalCode = postalCode;
        return this;
    }

    public UserAddressParametersBuilder WithLatitude(decimal? latitude)
    {
        _latitude = latitude;
        return this;
    }

    public UserAddressParametersBuilder WithLongitude(decimal? longitude)
    {
        _longitude = longitude;
        return this;
    }

    public UserAddress AddTo(User user) =>
        user.AddAddress(_addressId, _title, _receiverName, _phoneNumber, _province, _city, _address, _postalCode, _latitude, _longitude);

    public void UpdateOn(User user, bool isDefault = false) =>
        user.UpdateAddress(_addressId, _title, _receiverName, _phoneNumber, _province, _city, _address, _postalCode, isDefault, _latitude, _longitude);

    public UserAddressId AddressId => _addressId;
}
