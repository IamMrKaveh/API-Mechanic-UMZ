using Application.Auth.Features.Shared;
using Domain.Security.Aggregates;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Auth.Features.Commands.SendOtp;

public class SendOtpHandler(
    IUnitOfWork unitOfWork,
    IOtpService otpService,
    IOtpRepository otpRepository,
    IUserRepository userRepository,
    IInitialAdminOptions initialAdminOptions,
    IDateTimeProvider dateTimeProvider,
    IOptions<AuthOptions> authOptions)
    : ICommandHandler<SendOtpCommand>
{
    public async Task<ServiceResult> Handle(SendOtpCommand request, CancellationToken ct)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);

        var user = await userRepository.GetByPhoneNumberAsync(phoneNumber, ct);
        if (user is null)
        {
            // SEC-24 (design decision, documented): registration is phone-first -
            // verifying an OTP for an unknown number creates the account. The
            // response is identical to the existing-user path, so number
            // enumeration is not possible; the trade-off is that any valid
            // number can self-provision an account.
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

        // OTP length and expiration come from the Auth section (single source of
        // truth) so configured security behavior matches what actually happens.
        var options = authOptions.Value;
        var otpCode = OtpCode.Generate(options.OtpLength);
        var now = dateTimeProvider.UtcNow;
        var otp = UserOtp.Create(user.Id, otpCode, request.Purpose, TimeSpan.FromMinutes(options.OtpExpirationMinutes), now);
        await otpRepository.AddAsync(otp, ct);

        var sendResult = await otpService.SendOtpAsync(phoneNumber, otpCode, request.Purpose, ct);
        if (sendResult.IsFailure)
            return ServiceResult.Failure(sendResult.Error);

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