using Application.Auth.Contracts;
using Application.Common.Results;

namespace Application.Auth.Features.Commands.SendOtp;

public class SendOtpHandler(
    IAuthService authService,
    ILogger<SendOtpHandler> logger) : IRequestHandler<SendOtpCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        SendOtpCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("OTP request for {Phone}", request.PhoneNumber);
        return await authService.RequestOtpAsync(request.PhoneNumber, request.IpAddress, ct);
    }
}