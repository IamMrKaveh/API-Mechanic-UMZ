namespace Application.User.Contracts;

public interface IUserQueryService
{
    Task<UserProfileDto?> GetUserProfileAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<PaginatedResult<UserProfileDto>> GetUsersPagedAsync(
        string? search,
        bool? isActive,
        bool? isAdmin,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default
        );

    Task<IEnumerable<UserAddressDto>> GetUserAddressesAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<UserDashboardDto?> GetUserDashboardAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<PaginatedResult<ProductReviewDto>> GetUserReviewsPagedAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default
        );

    Task<PaginatedResult<WishlistItemDto>> GetUserWishlistPagedAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default
        );
}