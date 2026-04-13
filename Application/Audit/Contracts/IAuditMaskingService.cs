using Domain.User.ValueObjects;

namespace Application.Audit.Contracts;

public interface IAuditMaskingService
{
    string MaskPhoneNumber(PhoneNumber phoneNumber);

    string MaskEmail(Email email);

    string MaskIpAddress(IpAddress ipAddress);

    string MaskSensitiveData(string data);
}