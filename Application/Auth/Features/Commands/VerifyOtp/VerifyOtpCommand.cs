using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Commands.VerifyOtp;

public record VerifyOtpCommand : IRequest<ServiceResult<AuthResult>>
{
    public required string PhoneNumber { get; init; }
    public required string Code { get; init; }
    public required string IpAddress { get; init; }
    public string? UserAgent { get; init; }
}