using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.SendOtp;

public class SendOtpHandler(
    IOtpService otpService,
    IAuditService auditService) : IRequestHandler<SendOtpCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(SendOtpCommand request, CancellationToken ct)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        var otpCode = OtpCode.Generate(6);
        var result = await otpService.SendOtpAsync(phoneNumber, otpCode, request.Purpose, ct);

        if (result.IsFailed)
            return ServiceResult.Failure(result.Error);

        await auditService.LogSecurityEventAsync(
            "SendOtp",
            $"OTP برای شماره {request.PhoneNumber} ارسال شد.",
            IpAddress.Unknown,
            ct: ct);

        return ServiceResult.Success();
    }
}