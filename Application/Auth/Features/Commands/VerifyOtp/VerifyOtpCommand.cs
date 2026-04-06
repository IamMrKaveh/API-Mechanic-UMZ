using Application.Auth.Features.Shared;
using Application.Common.Results;

namespace Application.Auth.Features.Commands.VerifyOtp;

public record VerifyOtpCommand(
    string PhoneNumber,
    string Code,
    string IpAddress,
    string? UserAgent) : IRequest<ServiceResult<AuthResult>>;