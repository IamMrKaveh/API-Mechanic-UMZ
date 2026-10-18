using Application.Common.Interfaces;

namespace Application.Wallet.Features.Shared;

/// <summary>
/// Resolves the user a wallet query targets. Callers may only act on their
/// own wallet; an explicit target user is honored for admins only (defense in
/// depth behind the admin-only controllers).
/// </summary>
public static class WalletUserResolver
{
    /// <summary>
    /// Returns the user a wallet query targets, or <c>null</c> for "no user
    /// filter" (admin aggregate queries). Non-admin callers always resolve to
    /// their own id (their requested id is ignored).
    /// </summary>
    public static Guid? Resolve(ICurrentUserService currentUser, Guid? requestedUserId)
    {
        if (currentUser.IsAdmin)
        {
            if (requestedUserId.HasValue && requestedUserId.Value != Guid.Empty)
                return requestedUserId.Value;

            // No explicit target: admin without a user context keeps "no filter".
            return currentUser.UserId ?? requestedUserId;
        }

        return currentUser.UserId;
    }
}
