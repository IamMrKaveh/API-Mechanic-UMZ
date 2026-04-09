using Domain.User.Results;
using Domain.User.ValueObjects;

namespace Domain.User.Services;

public sealed class UserDomainService
{
    private const int MaxAddressesPerUser = 10;

    public LoginAttemptResult ValidateLoginAttempt(Aggregates.User user, string providedPasswordHash)
    {
        Guard.Against.Null(user, nameof(user));

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

        if (!user.IsActive)
            return Result.Failure(new Error(
                "400",
                "حساب کاربری غیرفعال است."));

        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure((new Error(
                "400",
                "نام الزامی است.")));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure((new Error(
                "400",
                "نام خانوادگی الزامی است.")));

        if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumberExists)
            return Result.Failure((new Error(
                "400",
                "این شماره تلفن قبلاً ثبت شده است.")));

        return Result.Success();
    }

    public Result ValidateEmailChange(
        Aggregates.User user,
        string newEmail,
        bool emailExists)
    {
        Guard.Against.Null(user, nameof(user));

        if (!user.IsActive)
            return Result.Failure((new Error(
                "400",
                "حساب کاربری غیرفعال است.")));

        if (string.IsNullOrWhiteSpace(newEmail))
            return Result.Failure((new Error(
                "400",
                "ایمیل الزامی است.")));

        if (emailExists)
            return Result.Failure((new Error(
                "400",
                "این ایمیل قبلاً ثبت شده است.")));

        return Result.Success();
    }

    public Result ValidateAddAddress(Aggregates.User user)
    {
        Guard.Against.Null(user, nameof(user));

        if (!user.IsActive)
            return Result.Failure((new Error(
                "400",
                "حساب کاربری غیرفعال است.")));

        var activeAddressCount = user.Addresses.Count;

        if (activeAddressCount >= MaxAddressesPerUser)
            return Result.Failure((new Error(
                "400",
                $"حداکثر تعداد آدرس مجاز {MaxAddressesPerUser} عدد است.")));

        return Result.Success();
    }

    public Result ValidateRemoveAddress(Aggregates.User user, UserAddressId addressId)
    {
        Guard.Against.Null(user, nameof(user));
        Guard.Against.Null(addressId, nameof(addressId));

        if (!user.HasAddress(addressId))
            return Result.Failure((new Error("400", "آدرس یافت نشد.")));

        return Result.Success();
    }

    public Result ValidateSetDefaultAddress(Aggregates.User user, UserAddressId addressId)
    {
        Guard.Against.Null(user, nameof(user));
        Guard.Against.Null(addressId, nameof(addressId));

        if (!user.IsActive)
            return Result.Failure((new Error("400", "حساب کاربری غیرفعال است.")));

        if (!user.HasAddress(addressId))
            return Result.Failure((new Error("400", "آدرس یافت نشد.")));

        return Result.Success();
    }

    public Result ValidateAdminPromotion(Aggregates.User requestingUser, Aggregates.User targetUser)
    {
        Guard.Against.Null(requestingUser, nameof(requestingUser));
        Guard.Against.Null(targetUser, nameof(targetUser));

        if (!requestingUser.IsAdmin)
            return Result.Failure((new Error("400", "فقط مدیران می‌توانند کاربران را ارتقا دهند.")));

        if (!targetUser.IsActive)
            return Result.Failure((new Error("400", "کاربر هدف غیرفعال است.")));

        if (targetUser.IsAdmin)
            return Result.Failure((new Error("400", "کاربر قبلاً مدیر است.")));

        return Result.Success();
    }

    public Result ValidateAdminDemotion(Aggregates.User requestingUser, Aggregates.User targetUser)
    {
        Guard.Against.Null(requestingUser, nameof(requestingUser));
        Guard.Against.Null(targetUser, nameof(targetUser));

        if (!requestingUser.IsAdmin)
            return Result.Failure((new Error("400", "فقط مدیران می‌توانند دسترسی ادمین را لغو کنند.")));

        if (requestingUser.Id == targetUser.Id)
            return Result.Failure((new Error("400", "نمی‌توانید دسترسی ادمین خودتان را لغو کنید.")));

        if (!targetUser.IsAdmin)
            return Result.Failure((new Error("400", "کاربر مدیر نیست.")));

        return Result.Success();
    }

    public Result ValidateDeleteUser(Aggregates.User requestingUser, Aggregates.User targetUser)
    {
        Guard.Against.Null(requestingUser, nameof(requestingUser));
        Guard.Against.Null(targetUser, nameof(targetUser));

        if (!requestingUser.IsAdmin)
            return Result.Failure((new Error("400", "فقط مدیران می‌توانند کاربران را حذف کنند.")));

        if (requestingUser.Id == targetUser.Id)
            return Result.Failure((new Error("400", "نمی‌توانید حساب خودتان را حذف کنید.")));

        return Result.Success();
    }

    public UserAddressId? ResolveEffectiveDefaultAddress(Aggregates.User user)
    {
        Guard.Against.Null(user, nameof(user));

        if (user.DefaultAddressId is not null)
        {
            var defaultAddress = user.Addresses.FirstOrDefault(a => a.Id == user.DefaultAddressId);
            if (defaultAddress is not null)
                return defaultAddress.Id;
        }

        var firstActive = user.Addresses.FirstOrDefault();
        return firstActive?.Id;
    }
}