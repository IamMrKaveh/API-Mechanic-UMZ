using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Communication.Contracts;

public interface ISmsService
{
    Task<bool> SendOtpSMSAsync(
        PhoneNumber phoneNumber,
        OtpCode code,
        CancellationToken ct = default);
}