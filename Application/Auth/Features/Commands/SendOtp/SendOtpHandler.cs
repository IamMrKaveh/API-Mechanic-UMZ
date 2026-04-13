using Domain.Common.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.SendOtp;

public class SendOtpHandler(
    IOtpService otpService,
    IAuditService auditService) : IRequestHandler<SendOtpCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(SendOtpCommand request, CancellationToken ct)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        var result = await otpService.SendOtpAsync(phoneNumber, request.Purpose, ct);

        if (!result.IsSuccess)
            return ServiceResult.Failure(result.Error.Message);

        await auditService.LogSecurityEventAsync(
            "SendOtp",
            $"OTP برای شماره {request.PhoneNumber} ارسال شد.",
            IpAddress.Unknown);

        return ServiceResult.Success();
    }
}