using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public class UserOtpBuilder
{
    private static readonly Faker Faker = new();

    private UserId _userId = UserId.NewId();
    private OtpCode _code = OtpCode.Create(Faker.Random.ReplaceNumbers("######"));
    private OtpPurpose _purpose = OtpPurpose.Login;
    private TimeSpan _validity = TimeSpan.FromMinutes(5);
    private PhoneNumber? _targetPhoneNumber;

    public UserOtpBuilder WithUserId(UserId userId)
    {
        _userId = userId;
        return this;
    }

    public UserOtpBuilder WithCode(OtpCode code)
    {
        _code = code;
        return this;
    }

    public UserOtpBuilder WithCode(string value)
    {
        _code = OtpCode.Create(value);
        return this;
    }

    public UserOtpBuilder WithPurpose(OtpPurpose purpose)
    {
        _purpose = purpose;
        return this;
    }

    public UserOtpBuilder WithValidity(TimeSpan validity)
    {
        _validity = validity;
        return this;
    }

    /// <summary>
    /// Issues the challenge as a phone-change challenge bound to <paramref name="target"/>.
    /// </summary>
    public UserOtpBuilder WithTargetPhoneNumber(PhoneNumber target)
    {
        _targetPhoneNumber = target;
        return this;
    }

    public UserOtpBuilder WithTargetPhoneNumber(string target) => WithTargetPhoneNumber(PhoneNumber.Create(target));

    public UserOtp Build(DateTime? now = null)
    {
        var timestamp = now ?? DateTime.UtcNow;

        return _targetPhoneNumber is null
            ? UserOtp.Create(_userId, _code, _purpose, _validity, timestamp)
            : UserOtp.CreateForPhoneChange(_userId, _code, _targetPhoneNumber, _validity, timestamp);
    }
}
