using Domain.Common.Exceptions;
using Domain.Common.Guards;
using Domain.User.Results;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using System;
using System.Linq;

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
            return Result.Failure(new Error("User.Inactive", "حساب کاربری غیرفعال است.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure(new Error("User.InvalidFirstName", "نام الزامی است.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure(new Error("User.InvalidLastName", "نام خانوادگی الزامی است.", ErrorType.Validation));

        if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumberExists)
            return Result.Failure(new Error("User.DuplicatePhone", "این شماره تلفن قبلاً ثبت شده است.", ErrorType.Conflict));

        return Result.Success();
    }

    public Result ValidateEmailChange(
        Aggregates.User user,
        string newEmail,
        bool emailExists)
    {
        Guard.Against.Null(user, nameof(user));

        if (!user.IsActive)
            return Result.Failure(new Error("User.Inactive", "حساب کاربری غیرفعال است.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(newEmail))
            return Result.Failure(new Error("User.InvalidEmail", "ایمیل الزامی است.", ErrorType.Validation));

        if (emailExists)
            return Result.Failure(new Error("User.DuplicateEmail", "این ایمیل قبلاً ثبت شده است.", ErrorType.Conflict));

        return Result.Success();
    }

    public Result ValidateAddAddress(Aggregates.User user)
    {
        Guard.Against.Null(user, nameof(user));

        if (!user.IsActive)
            return Result.Failure(new Error("User.Inactive", "حساب کاربری غیرفعال است.", ErrorType.Validation));

        var activeAddressCount = user.Addresses.Count;

        if (activeAddressCount >= MaxAddressesPerUser)
            return Result.Failure(new Error("User.MaxAddressesExceeded", $"حداکثر تعداد آدرس مجاز {MaxAddressesPerUser} عدد است.", ErrorType.Validation));

        return Result.Success();
    }

    public Result ValidateRemoveAddress(Aggregates.User user, UserAddressId addressId)
    {
        Guard.Against.Null(user, nameof(user));
        Guard.Against.Null(addressId, nameof(addressId));

        if (!user.HasAddress(addressId))
            return Result.Failure(new Error("User.AddressNotFound", "آدرس یافت نشد.", ErrorType.NotFound));

        return Result.Success();
    }

    public Result ValidateSetDefaultAddress(Aggregates.User user, UserAddressId addressId)
    {
        Guard.Against.Null(user, nameof(user));
        Guard.Against.Null(addressId, nameof(addressId));

        if (!user.IsActive)
            return Result.Failure(new Error("User.Inactive", "حساب کاربری غیرفعال است.", ErrorType.Validation));

        if (!user.HasAddress(addressId))
            return Result.Failure(new Error("User.AddressNotFound", "آدرس یافت نشد.", ErrorType.NotFound));

        return Result.Success();
    }

    public Result ValidateAdminPromotion(Aggregates.User requestingUser, Aggregates.User targetUser)
    {
        Guard.Against.Null(requestingUser, nameof(requestingUser));
        Guard.Against.Null(targetUser, nameof(targetUser));

        if (!requestingUser.IsAdmin)
            return Result.Failure(new Error("User.Unauthorized", "فقط مدیران می‌توانند کاربران را ارتقا دهند.", ErrorType.Forbidden));

        if (!targetUser.IsActive)
            return Result.Failure(new Error("User.TargetInactive", "کاربر هدف غیرفعال است.", ErrorType.Validation));

        if (targetUser.IsAdmin)
            return Result.Failure(new Error("User.AlreadyAdmin", "کاربر قبلاً مدیر است.", ErrorType.Conflict));

        return Result.Success();
    }

    public Result ValidateAdminDemotion(Aggregates.User requestingUser, Aggregates.User targetUser)
    {
        Guard.Against.Null(requestingUser, nameof(requestingUser));
        Guard.Against.Null(targetUser, nameof(targetUser));

        if (!requestingUser.IsAdmin)
            return Result.Failure(new Error("User.Unauthorized", "فقط مدیران می‌توانند دسترسی ادمین را لغو کنند.", ErrorType.Forbidden));

        if (requestingUser.Id == targetUser.Id)
            return Result.Failure(new Error("User.SelfDemotion", "نمی‌توانید دسترسی ادمین خودتان را لغو کنید.", ErrorType.Validation));

        if (!targetUser.IsAdmin)
            return Result.Failure(new Error("User.NotAdmin", "کاربر مدیر نیست.", ErrorType.Validation));

        return Result.Success();
    }

    public Result ValidateDeleteUser(Aggregates.User requestingUser, Aggregates.User targetUser)
    {
        Guard.Against.Null(requestingUser, nameof(requestingUser));
        Guard.Against.Null(targetUser, nameof(targetUser));

        if (!requestingUser.IsAdmin)
            return Result.Failure(new Error("User.Unauthorized", "فقط مدیران می‌توانند کاربران را حذف کنند.", ErrorType.Forbidden));

        if (requestingUser.Id == targetUser.Id)
            return Result.Failure(new Error("User.SelfDeletion", "نمی‌توانید حساب خودتان را حذف کنید.", ErrorType.Validation));

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