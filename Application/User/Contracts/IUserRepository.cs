namespace Application.User.Contracts;

public interface IUserRepository
{
    
    Task<Domain.User.User?> GetByIdAsync(
        int id,
        CancellationToken ct = default
        );

    Task<Domain.User.User?> GetByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken ct = default
        );

    Task<Domain.User.User?> GetWithOtpsAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<Domain.User.User?> GetWithAddressesAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<Domain.User.User?> GetWithSessionsAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<Domain.User.User?> GetByIdIncludingDeletedAsync(
        int id,
        CancellationToken ct = default
        );

    Task AddAsync(
        Domain.User.User user,
        CancellationToken ct = default
        );

    void Update(
        Domain.User.User user
        );

    Task<(IEnumerable<Domain.User.User> Users, int TotalCount)> GetPagedAsync(
        string? search,
        bool? isActive,
        bool? isAdmin,
        int page,
        int pageSize,
        CancellationToken ct = default
        );

    Task<bool> ExistsByPhoneNumberAsync(
        string phoneNumber,
        int? excludeUserId = null,
        CancellationToken ct = default
        );

    
    Task<UserOtp?> GetActiveOtpAsync(
        int userId,
        CancellationToken ct = default
        );

    Task AddUserOtpAsync(
        UserOtp otp,
        CancellationToken ct = default
        );

    Task DeleteUserOtpsAsync(
        int userId,
        CancellationToken ct = default
        );

    
    Task<UserSession?> GetSessionBySelectorAsync(
        string tokenSelector,
        CancellationToken ct = default
        );

    Task AddSessionAsync(
        UserSession session,
        CancellationToken ct = default
        );

    Task RevokeSessionAsync(
        int sessionId,
        CancellationToken ct = default
        );

    Task RevokeAllUserSessionsAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(
        int userId,
        CancellationToken ct = default
        );

    
    Task<bool> IsInWishlistAsync(
        int userId,
        int productId,
        CancellationToken ct = default
        );

    Task AddToWishlistAsync(
        int userId,
        int productId,
        CancellationToken ct = default
        );

    Task RemoveFromWishlistAsync(
        int userId,
        int productId,
        CancellationToken ct = default
        );

    Task<(object users, int totalItems)> GetUsersAsync(
        bool includeDeleted,
        int page,
        int pageSize
        );

    void UpdateUser(
        Domain.User.User user
        );

    Task AddUserAsync(
        Domain.User.User user
        );

    Task<bool> PhoneNumberExistsAsync(
        string phoneNumber,
        int userId,
        CancellationToken ct = default
        );

    Task<UserAddress?> GetUserAddressAsync(
        int userAddressId,
        CancellationToken cancellationToken
        );

    Task AddAddressAsync(
        UserAddress userAddress,
        CancellationToken ct
        );

    IQueryable<Domain.User.User> GetUsersQuery(
        bool includeDeleted
        );
}