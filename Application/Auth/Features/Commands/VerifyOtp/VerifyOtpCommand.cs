using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Commands.VerifyOtp;

public record VerifyOtpCommand(
    string PhoneNumber,
    string Code,
    string IpAddress,
    string? UserAgent) : IRequest<ServiceResult<AuthResult>>;