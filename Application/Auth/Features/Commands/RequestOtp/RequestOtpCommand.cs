namespace Application.Auth.Features.Commands.RequestOtp;

public record RequestOtpCommand : IRequest<ServiceResult>
{
    public required string PhoneNumber { get; init; }
    public required string IpAddress { get; init; }
}