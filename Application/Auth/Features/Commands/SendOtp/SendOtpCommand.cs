using Domain.Security.Enums;

namespace Application.Auth.Features.Commands.SendOtp;

public record SendOtpCommand(
    string PhoneNumber,
    string IpAddress,
    OtpPurpose Purpose = OtpPurpose.Login) : IRequest<ServiceResult>;