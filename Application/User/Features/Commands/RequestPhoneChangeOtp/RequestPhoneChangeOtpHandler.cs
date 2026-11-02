using Application.Auth.Features.Shared;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;
using SharedKernel.Abstractions.Interfaces;

namespace Application.User.Features.Commands.RequestPhoneChangeOtp;

/// <summary>
/// Issues the OTP that <see cref="Commands.ChangePhoneNumber.ChangePhoneNumberHandler"/>
/// consumes. The challenge is bound to the requested number, so a code can only
/// be redeemed for the number it was sent to.
/// </summary>
public class RequestPhoneChangeOtpHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IOtpService otpService,
    IOtpRepository otpRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IOptions<AuthOptions> authOptions)
    : ICommandHandler<RequestPhoneChangeOtpCommand>
{
    public async Task<ServiceResult> Handle(
        RequestPhoneChangeOtpCommand request,
        CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return ServiceResult.Unauthorized();

        var userId = UserId.From(currentUser.UserId.Value);

        var phoneResult = PhoneNumber.TryCreate(request.NewPhoneNumber);
        if (phoneResult.IsFailure)
            return ServiceResult.Failure(phoneResult.Error);

        var phone = phoneResult.Value;

        if (await userRepository.ExistsByPhoneNumberAsync(phone, userId, ct))
            return ServiceResult.Conflict("این شماره تلفن قبلاً ثبت شده است.");

        var rateLimitOk = await otpService.ValidateRateLimitAsync(userId, OtpPurpose.PhoneVerification, ct);
        if (!rateLimitOk)
            return ServiceResult.Failure("تعداد درخواست OTP بیش از حد مجاز است. لطفاً بعداً تلاش کنید.");

        var options = authOptions.Value;
        var otpCode = OtpCode.Generate(options.OtpLength);
        var now = dateTimeProvider.UtcNow;
        var otp = UserOtp.CreateForPhoneChange(
            userId,
            otpCode,
            phone,
            TimeSpan.FromMinutes(options.OtpExpirationMinutes),
            now);

        await otpRepository.AddAsync(otp, ct);

        var sendResult = await otpService.SendOtpAsync(phone, otpCode, OtpPurpose.PhoneVerification, ct);
        if (sendResult.IsFailure)
            return ServiceResult.Failure(sendResult.Error);

        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}
