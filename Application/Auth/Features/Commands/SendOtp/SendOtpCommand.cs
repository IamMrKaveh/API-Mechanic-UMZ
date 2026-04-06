using Application.Common.Results;

namespace Application.Auth.Features.Commands.SendOtp;

public record SendOtpCommand(
    string PhoneNumber,
    string IpAddress) : IRequest<ServiceResult>;