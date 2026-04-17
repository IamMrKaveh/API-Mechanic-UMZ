using Application.Review.Features.Shared;
using Application.User.Contracts;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Infrastructure.User.QueryServices;

public sealed class UserQueryService(DBContext context) : IUserQueryService
{
    public async Task<UserProfileDto?> GetUserProfileAsync(UserId userId, CancellationToken ct = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileDto
            {
                Id = u.Id.Value,
                FirstName = u.FullName.FirstName,
                LastName = u.FullName.LastName,
                Email = u.Email.Value,
                PhoneNumber = u.PhoneNumber != null ? u.PhoneNumber.Value : "",
                IsActive = u.IsActive,
                IsAdmin = u.IsAdmin,
                IsEmailVerified = u.IsEmailVerified,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        return user;
    }

    public async Task<IReadOnlyList<UserAddressDto>> GetUserAddressesAsync(
        UserId userId, CancellationToken ct = default)
    {
        var addresses = await context.UserAddresses
            .AsNoTracking()
            .Where(a => EF.Property<Guid>(a, "UserId") == userId.Value)
            .Select(a => new UserAddressDto
            {
                Id = a.Id.Value,
                Title = a.Title,
                ReceiverName = a.ReceiverName,
                PhoneNumber = a.PhoneNumber,
                Province = a.Province,
                City = a.City,
                PostalCode = a.PostalCode,
                IsDefault = a.IsDefault
            })
            .ToListAsync(ct);

        return addresses.AsReadOnly();
    }

    public async Task<UserDashboardDto> GetUserDashboardAsync(
        UserId userId, CancellationToken ct = default)
    {
        var orderCount = await context.Orders
            .AsNoTracking()
            .CountAsync(o => o.UserId == userId, ct);

        var completedOrderCount = await context.Orders
            .AsNoTracking()
            .CountAsync(o => o.UserId == userId && o.Status == Domain.Order.ValueObjects.OrderStatusValue.Delivered, ct);

        var wishlistCount = await context.Wishlists
            .AsNoTracking()
            .CountAsync(w => w.UserId == userId, ct);

        var ticketCount = await context.Tickets
            .AsNoTracking()
            .CountAsync(t => t.CustomerId == userId, ct);

        return new UserDashboardDto
        {
            TotalOrders = orderCount,
            CompletedOrders = completedOrderCount,
            WishlistCount = wishlistCount,
            OpenTickets = ticketCount
        };
    }

    public Task<PaginatedResult<UserProfileDto>> GetUsersPagedAsync(string? search, bool? isActive, bool? isAdmin, bool includeDeleted, int page, int pageSize, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    Task<IEnumerable<UserAddressDto>> IUserQueryService.GetUserAddressesAsync(UserId userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(UserId userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedResult<ProductReviewDto>> GetUserReviewsPagedAsync(UserId userId, int page, int pageSize, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}