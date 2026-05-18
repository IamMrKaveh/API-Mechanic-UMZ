using Domain.Security.Aggregates;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.SendOtp;

public class SendOtpHandler(
    IOtpService otpService,
    IOtpRepository otpRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<SendOtpCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(SendOtpCommand request, CancellationToken ct)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);

        var user = await userRepository.GetByPhoneNumberAsync(phoneNumber, ct);
        if (user is null)
        {
            user = Domain.User.Aggregates.User.RegisterByPhone(phoneNumber);
            await userRepository.AddAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        var rateLimitOk = await otpService.ValidateRateLimitAsync(user.Id, request.Purpose, ct);
        if (!rateLimitOk)
            return ServiceResult.Failure("تعداد درخواست OTP بیش از حد مجاز است. لطفاً بعداً تلاش کنید.");

        var otpCode = OtpCode.Generate(6);

        var otp = UserOtp.Create(
            user.Id,
            otpCode,
            request.Purpose,
            TimeSpan.FromMinutes(2));

        await otpRepository.AddAsync(otp, ct);

        var sendResult = await otpService.SendOtpAsync(phoneNumber, otpCode, request.Purpose, ct);
        if (sendResult.IsFailed)
            return ServiceResult.Failure(sendResult.Error);

        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogSecurityEventAsync(
            "SendOtp",
            $"OTP برای شماره {request.PhoneNumber} ارسال شد.",
            IpAddress.Unknown,
            ct: ct);

        return ServiceResult.Success();
    }
}