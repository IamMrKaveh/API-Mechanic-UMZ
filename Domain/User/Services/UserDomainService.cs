using Domain.User.Results;
using Domain.User.ValueObjects;

namespace Domain.User.Services;

public sealed class UserDomainService
{
    private const int MaxAddressesPerUser = 10;

    public LoginAttemptResult ValidateLoginAttempt(Aggregates.User user, string providedPasswordHash)
    {
        Guard.Against.Null(user, nameof(user));

        if (user.IsDeleted)
            return LoginAttemptResult.AccountDeleted();

        if (!user.IsActive)
            return LoginAttemptResult.AccountInactive();

        if (user.IsLockedOut)
        {
            var unlockTime = user.LockoutEnd!.Value;
            var remaining = unlockTime - DateTime.UtcNow;
            return LoginAttemptResult.AccountLocked(unlockTime, remaining);
        }

        if (!string.Equals(user.PasswordHash, providedPasswordHash, StringComparison.Ordinal))
        {
            user.RecordFailedLogin();
            return LoginAttemptResult.InvalidCredentials(user.GetRemainingLoginAttempts());
        }

        user.RecordSuccessfulLogin();
        return LoginAttemptResult.Success(user.Id);
    }

    public Result ValidateProfileUpdate(
        Aggregates.User user,
        string firstName,
        string lastName,
        string? phoneNumber,
        bool phoneNumberExists)
    {
        Guard.Against.Null(user, nameof(user));

        if (user.IsDeleted)
            return Result.Failure("کاربر حذف شده است.");

        if (!user.IsActive)
            return Result.Failure("حساب کاربری غیرفعال است.");

        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure("نام الزامی است.");

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure("نام خانوادگی الزامی است.");

        if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumberExists)
            return Result.Failure("این شماره تلفن قبلاً ثبت شده است.");

        return Result.Success();
    }

    public Result ValidateEmailChange(
        Aggregates.User user,
        string newEmail,
        bool emailExists)
    {
        Guard.Against.Null(user, nameof(user));

        if (user.IsDeleted)
            return Result.Failure("کاربر حذف شده است.");

        if (!user.IsActive)
            return Result.Failure("حساب کاربری غیرفعال است.");

        if (string.IsNullOrWhiteSpace(newEmail))
            return Result.Failure("ایمیل الزامی است.");

        if (emailExists)
            return Result.Failure("این ایمیل قبلاً ثبت شده است.");

        return Result.Success();
    }

    public Result ValidateAddAddress(Aggregates.User user)
    {
        Guard.Against.Null(user, nameof(user));

        if (user.IsDeleted)
            return Result.Failure("کاربر حذف شده است.");

        if (!user.IsActive)
            return Result.Failure("حساب کاربری غیرفعال است.");

        var activeAddressCount = user.Addresses.Count(a => !a.IsDeleted);

        if (activeAddressCount >= MaxAddressesPerUser)
            return Result.Failure($"حداکثر تعداد آدرس مجاز {MaxAddressesPerUser} عدد است.");

        return Result.Success();
    }

    public Result ValidateRemoveAddress(Aggregates.User user, UserAddressId addressId)
    {
        Guard.Against.Null(user, nameof(user));
        Guard.Against.Null(addressId, nameof(addressId));

        if (user.IsDeleted)
            return Result.Failure("کاربر حذف شده است.");

        if (!user.HasAddress(addressId))
            return Result.Failure("آدرس یافت نشد.");

        return Result.Success();
    }

    public Result ValidateSetDefaultAddress(Aggregates.User user, UserAddressId addressId)
    {
        Guard.Against.Null(user, nameof(user));
        Guard.Against.Null(addressId, nameof(addressId));

        if (user.IsDeleted)
            return Result.Failure("کاربر حذف شده است.");

        if (!user.IsActive)
            return Result.Failure("حساب کاربری غیرفعال است.");

        if (!user.HasAddress(addressId))
            return Result.Failure("آدرس یافت نشد.");

        return Result.Success();
    }

    public Result ValidateAdminPromotion(Aggregates.User requestingUser, Aggregates.User targetUser)
    {
        Guard.Against.Null(requestingUser, nameof(requestingUser));
        Guard.Against.Null(targetUser, nameof(targetUser));

        if (!requestingUser.IsAdmin)
            return Result.Failure("فقط مدیران می‌توانند کاربران را ارتقا دهند.");

        if (targetUser.IsDeleted)
            return Result.Failure("کاربر هدف حذف شده است.");

        if (!targetUser.IsActive)
            return Result.Failure("کاربر هدف غیرفعال است.");

        if (targetUser.IsAdmin)
            return Result.Failure("کاربر قبلاً مدیر است.");

        return Result.Success();
    }

    public Result ValidateAdminDemotion(Aggregates.User requestingUser, Aggregates.User targetUser)
    {
        Guard.Against.Null(requestingUser, nameof(requestingUser));
        Guard.Against.Null(targetUser, nameof(targetUser));

        if (!requestingUser.IsAdmin)
            return Result.Failure("فقط مدیران می‌توانند دسترسی ادمین را لغو کنند.");

        if (requestingUser.Id == targetUser.Id)
            return Result.Failure("نمی‌توانید دسترسی ادمین خودتان را لغو کنید.");

        if (!targetUser.IsAdmin)
            return Result.Failure("کاربر مدیر نیست.");

        return Result.Success();
    }

    public Result ValidateDeleteUser(Aggregates.User requestingUser, Aggregates.User targetUser)
    {
        Guard.Against.Null(requestingUser, nameof(requestingUser));
        Guard.Against.Null(targetUser, nameof(targetUser));

        if (!requestingUser.IsAdmin)
            return Result.Failure("فقط مدیران می‌توانند کاربران را حذف کنند.");

        if (targetUser.IsDeleted)
            return Result.Failure("کاربر قبلاً حذف شده است.");

        if (requestingUser.Id == targetUser.Id)
            return Result.Failure("نمی‌توانید حساب خودتان را حذف کنید.");

        return Result.Success();
    }

    public UserAddressId? ResolveEffectiveDefaultAddress(Aggregates.User user)
    {
        Guard.Against.Null(user, nameof(user));

        if (user.DefaultAddressId.HasValue)
        {
            var defaultAddress = user.Addresses.FirstOrDefault(a => a.Id == user.DefaultAddressId && !a.IsDeleted);
            if (defaultAddress is not null)
                return defaultAddress.Id;
        }

        var firstActive = user.Addresses.FirstOrDefault(a => !a.IsDeleted);
        return firstActive?.Id;
    }
}