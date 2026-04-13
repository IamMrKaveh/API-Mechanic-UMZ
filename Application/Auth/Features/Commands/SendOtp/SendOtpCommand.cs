using Domain.Security.Enums;

namespace Application.Auth.Features.Commands.SendOtp;

public record SendOtpCommand(
    string otpCode,
    string PhoneNumber,
    string IpAddress,
    OtpPurpose Purpose = OtpPurpose.Login) : IRequest<ServiceResult>;