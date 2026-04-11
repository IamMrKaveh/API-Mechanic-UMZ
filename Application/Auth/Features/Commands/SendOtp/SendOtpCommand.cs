namespace Application.Auth.Features.Commands.SendOtp;

public record SendOtpCommand(
    string PhoneNumber,
    string IpAddress) : IRequest<ServiceResult>;