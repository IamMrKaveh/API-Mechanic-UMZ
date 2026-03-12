namespace Domain.User.Results;

public sealed class LoginAttemptResult
{
    public bool IsSuccess { get; private set; }
    public bool IsAccountLocked { get; private set; }
    public bool IsAccountInactive { get; private set; }
    public bool IsAccountDeleted { get; private set; }
    public bool IsInvalidCredentials { get; private set; }
    public UserId? UserId { get; private set; }
    public string? Error { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public TimeSpan? LockoutRemaining { get; private set; }
    public int? RemainingAttempts { get; private set; }

    private LoginAttemptResult()
    { }

    public static LoginAttemptResult Success(UserId userId) =>
        new()
        {
            IsSuccess = true,
            UserId = userId
        };

    public static LoginAttemptResult AccountLocked(DateTime lockoutEnd, TimeSpan remaining) =>
        new()
        {
            IsSuccess = false,
            IsAccountLocked = true,
            LockoutEnd = lockoutEnd,
            LockoutRemaining = remaining,
            Error = $"حساب کاربری قفل شده است. تا {(int)remaining.TotalMinutes} دقیقه دیگر امکان ورود وجود ندارد."
        };

    public static LoginAttemptResult AccountInactive() =>
        new()
        {
            IsSuccess = false,
            IsAccountInactive = true,
            Error = "حساب کاربری غیرفعال است."
        };

    public static LoginAttemptResult AccountDeleted() =>
        new()
        {
            IsSuccess = false,
            IsAccountDeleted = true,
            Error = "حساب کاربری یافت نشد."
        };

    public static LoginAttemptResult InvalidCredentials(int remainingAttempts) =>
        new()
        {
            IsSuccess = false,
            IsInvalidCredentials = true,
            RemainingAttempts = remainingAttempts,
            Error = remainingAttempts > 0
                ? $"رمز عبور اشتباه است. {remainingAttempts} تلاش باقی مانده."
                : "رمز عبور اشتباه است. حساب شما قفل شد."
        };
}