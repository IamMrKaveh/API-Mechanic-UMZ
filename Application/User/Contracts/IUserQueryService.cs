using Application.Review.Features.Shared;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Models;

namespace Application.User.Contracts;

public interface IUserQueryService
{
    Task<UserProfileDto?> GetUserProfileAsync(UserId userId, CancellationToken ct = default);

    Task<PaginatedResult<UserProfileDto>> GetUsersPagedAsync(
        string? search,
        bool? isActive,
        bool? isAdmin,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IEnumerable<UserAddressDto>> GetUserAddressesAsync(UserId userId, CancellationToken ct = default);

    Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(UserId userId, CancellationToken ct = default);

    Task<UserDashboardDto?> GetUserDashboardAsync(UserId userId, CancellationToken ct = default);

    Task<PaginatedResult<ProductReviewDto>> GetUserReviewsPagedAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);
}