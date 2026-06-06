using Application.Auth.Features.Shared;
using Domain.Security.Enums;

namespace Application.Auth.Features.Commands.VerifyOtp;

public record VerifyOtpCommand(
    string PhoneNumber,
    string Code,
    OtpPurpose Purpose = OtpPurpose.Login) : IRequest<ServiceResult<AuthResult>>;