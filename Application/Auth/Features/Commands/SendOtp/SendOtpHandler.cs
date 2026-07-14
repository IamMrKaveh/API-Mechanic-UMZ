using Domain.Security.Aggregates;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.SendOtp;

public class SendOtpHandler(
    IUnitOfWork unitOfWork,
    IOtpService otpService,
    IOtpRepository otpRepository,
    IUserRepository userRepository,
    IInitialAdminOptions initialAdminOptions)
    : ICommandHandler<SendOtpCommand>
{
    public async Task<ServiceResult> Handle(SendOtpCommand request, CancellationToken ct)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);

        var user = await userRepository.GetByPhoneNumberAsync(phoneNumber, ct);
        if (user is null)
        {
            user = Domain.User.Aggregates.User.RegisterByPhone(phoneNumber);

            if (IsInitialAdmin(phoneNumber.Value))
            {
                user.PromoteToAdmin();
            }

            await userRepository.AddAsync(user, ct);
        }

        var rateLimitOk = await otpService.ValidateRateLimitAsync(user.Id, request.Purpose, ct);
        if (!rateLimitOk)
            return ServiceResult.Failure("تعداد درخواست OTP بیش از حد مجاز است. لطفاً بعداً تلاش کنید.");

        var otpCode = OtpCode.Generate(6);
        var otp = UserOtp.Create(user.Id, otpCode, request.Purpose, TimeSpan.FromMinutes(2));
        await otpRepository.AddAsync(otp, ct);

        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    private bool IsInitialAdmin(string phoneNumberValue)
    {
        if (initialAdminOptions.PhoneNumbers.Count == 0)
            return false;

        foreach (var configured in initialAdminOptions.PhoneNumbers)
        {
            if (string.IsNullOrWhiteSpace(configured))
                continue;

            if (string.Equals(configured.Trim(), phoneNumberValue, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}