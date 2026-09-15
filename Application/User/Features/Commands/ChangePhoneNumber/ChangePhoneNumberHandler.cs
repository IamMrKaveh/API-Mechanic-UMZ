using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.User.Features.Commands.ChangePhoneNumber;

public class ChangePhoneNumberHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    ICurrentUserService currentUser,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ChangePhoneNumberCommand>
{
    public async Task<ServiceResult> Handle(
        ChangePhoneNumberCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(currentUser.UserId!.Value);

        var result = PhoneNumber.TryCreate(request.NewPhoneNumber);
        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        var phoneNumber = result.Value;

        if (await userRepository.ExistsByPhoneNumberAsync(phoneNumber, userId, ct))
            return ServiceResult.Conflict("این شماره تلفن قبلاً ثبت شده است.");

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.NotFound("کاربر یافت نشد.");

        var otpCode = OtpCode.Create(request.OtpCode);

        var otp = await otpRepository.GetLatestActiveByUserIdAsync(userId, OtpPurpose.PhoneVerification, ct);
        if (otp is null)
            return ServiceResult.Failure("کد OTP فعالی یافت نشد.");

        var now = dateTimeProvider.UtcNow;
        try
        {
            otp.Verify(otpCode, now);
        }
        catch (DomainException ex)
        {
            otpRepository.Update(otp);
            return ServiceResult.Failure(ex.Message);
        }

        otpRepository.Update(otp);

        try
        {
            user.ChangePhoneNumber(phoneNumber);

            userRepository.Update(user);

            await auditService.LogSecurityEventAsync(
                "PhoneNumberChanged",
                $"شماره تلفن کاربر {userId} تغییر کرد.",
                IpAddress.System,
                userId,
                ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}