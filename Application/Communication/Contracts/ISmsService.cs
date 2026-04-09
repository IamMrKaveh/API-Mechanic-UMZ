using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Communication.Contracts;

public interface ISmsService
{
    Task<bool> SendOtpSMSAsync(
        PhoneNumber phoneNumber,
        OtpCode code,
        CancellationToken ct = default);

    Task<bool> SendSMSAsync(
        PhoneNumber phoneNumber,
        string message,
        CancellationToken ct = default);

    Task<bool> SendTemplateSMSAsync(
        PhoneNumber phoneNumber,
        string templateName,
        Dictionary<string, string> parameters,
        CancellationToken ct = default);
}