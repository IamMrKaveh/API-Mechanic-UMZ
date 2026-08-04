using Domain.User.ValueObjects;

namespace Application.Common.Authorization;

public static class AuthorizationGuard
{
    public static ServiceResult EnsureAuthenticated(ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null || currentUser.UserId.Value == Guid.Empty)
            return ServiceResult.Unauthorized("برای انجام این عملیات ابتدا وارد شوید.");

        return ServiceResult.Success();
    }

    public static ServiceResult EnsureOwnerOrAdmin(ICurrentUserService currentUser, Guid ownerUserId)
    {
        var auth = EnsureAuthenticated(currentUser);
        if (auth.IsFailure) return auth;

        if (currentUser.IsAdmin) return ServiceResult.Success();

        if (currentUser.UserId!.Value != ownerUserId)
            return ServiceResult.Forbidden("دسترسی به منابع کاربر دیگر مجاز نیست.");

        return ServiceResult.Success();
    }

    public static ServiceResult EnsureOwnerOrAdmin(ICurrentUserService currentUser, UserId ownerUserId)
        => EnsureOwnerOrAdmin(currentUser, ownerUserId.Value);

    public static ServiceResult EnsureAdmin(ICurrentUserService currentUser)
    {
        var auth = EnsureAuthenticated(currentUser);
        if (auth.IsFailure) return auth;

        if (!currentUser.IsAdmin)
            return ServiceResult.Forbidden("این عملیات فقط برای مدیران مجاز است.");

        return ServiceResult.Success();
    }

    public static ServiceResult<T> EnsureOwnerOrAdmin<T>(ICurrentUserService currentUser, Guid ownerUserId)
    {
        var auth = EnsureAuthenticated(currentUser);
        if (auth.IsFailure) return ServiceResult<T>.Failure(auth.Error);

        if (currentUser.IsAdmin) return ServiceResult<T>.Success(default!);

        if (currentUser.UserId!.Value != ownerUserId)
            return ServiceResult<T>.Forbidden("دسترسی به منابع کاربر دیگر مجاز نیست.");

        return ServiceResult<T>.Success(default!);
    }

    public static ServiceResult<T> EnsureAuthenticated<T>(ICurrentUserService currentUser)
    {
        var auth = EnsureAuthenticated(currentUser);
        return auth.IsFailure
            ? ServiceResult<T>.Failure(auth.Error)
            : ServiceResult<T>.Success(default!);
    }
}
