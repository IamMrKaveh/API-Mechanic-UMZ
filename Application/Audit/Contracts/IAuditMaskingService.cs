namespace Application.Audit.Contracts;

public interface IAuditMaskingService
{
    string MaskPhoneNumber(string phoneNumber);

    string MaskEmail(string email);

    string MaskIpAddress(string ipAddress);

    string MaskSensitiveData(string data);
}