using Domain.Common.ValueObjects;
using Domain.User.ValueObjects;

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

        var phoneNumberResult = PhoneNumber.TryCreate(request.PhoneNumber);
        if (phoneNumberResult.IsFailure)
            return ServiceResult.Validation(phoneNumberResult.Error.Message);

        var ipAddress = IpAddress.Create(request.IpAddress);

        return await authService.RequestOtpAsync(phoneNumberResult.Value, ipAddress, ct);
    }
}