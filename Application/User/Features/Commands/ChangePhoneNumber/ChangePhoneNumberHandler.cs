using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangePhoneNumber;

public class ChangePhoneNumberHandler(
    IUserRepository userRepository,
    IOtpService otpService,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<ChangePhoneNumberCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        ChangePhoneNumberCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

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
        otpService.HashOtp(otpCode);

        var isValid = await otpService.VerifyOtpAsync(phoneNumber, otpCode, OtpPurpose.PhoneVerification, ct);
        if (!isValid)
            return ServiceResult.Failure("کد تأیید نادرست است.");

        try
        {
            user.ChangePhoneNumber(phoneNumber);

            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(ct);

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