using Application.Auth.Features.Shared;
using Domain.Security.Enums;

namespace Application.Auth.Features.Commands.VerifyOtp;

public record VerifyOtpCommand(
    string PhoneNumber,
    string Code,
    string? DeviceInfo = null,
    OtpPurpose Purpose = OtpPurpose.Login) : ICommand<AuthResult>;