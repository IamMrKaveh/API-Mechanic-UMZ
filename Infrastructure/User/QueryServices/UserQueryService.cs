using Application.Auth.Features.Shared;
using Application.Review.Features.Shared;
using Application.User.Contracts;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Infrastructure.User.QueryServices;

public sealed class UserQueryService(DBContext context) : IUserQueryService
{
    public async Task<UserProfileDto?> GetUserProfileAsync(UserId userId, CancellationToken ct = default)
    {
        return await context.Users
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
    }

    public async Task<UserDashboardDto?> GetUserDashboardAsync(UserId userId, CancellationToken ct = default)
    {
        var profile = await GetUserProfileAsync(userId, ct);

        if (profile is null)
            return null;

        var orderCount = await context.Orders
            .AsNoTracking()
            .CountAsync(o => o.UserId == userId, ct);

        var completedOrderCount = await context.Orders
            .AsNoTracking()
            .CountAsync(o => o.UserId == userId && o.Status == Domain.Order.ValueObjects.OrderStatusValue.Delivered, ct);

        var totalSpent = await context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.Status == Domain.Order.ValueObjects.OrderStatusValue.Delivered)
            .SumAsync(o => (decimal?)o.FinalAmount.Amount, ct) ?? 0m;

        var wishlistCount = await context.Wishlists
            .AsNoTracking()
            .CountAsync(w => w.UserId == userId, ct);

        var ticketCount = await context.Tickets
            .AsNoTracking()
            .CountAsync(t => t.CustomerId == userId, ct);

        var addressCount = await context.UserAddresses
            .AsNoTracking()
            .CountAsync(a => a.UserId == userId, ct);

        return new UserDashboardDto
        {
            UserProfile = profile,
            TotalOrders = orderCount,
            CompletedOrders = completedOrderCount,
            DeliveredOrders = completedOrderCount,
            TotalSpent = totalSpent,
            WishlistCount = wishlistCount,
            OpenTickets = ticketCount,
            OpenTicketsCount = ticketCount,
            ActiveAddresses = addressCount,
            MemberSince = profile.CreatedAt,
            LastLoginAt = profile.LastLoginAt
        };
    }

    public async Task<PaginatedResult<UserProfileDto>> GetUsersPagedAsync(
        string? search,
        bool? isActive,
        bool? isAdmin,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.Value.ToLower().Contains(term) ||
                u.FullName.FirstName.ToLower().Contains(term) ||
                u.FullName.LastName.ToLower().Contains(term) ||
                (u.PhoneNumber != null && u.PhoneNumber.Value.Contains(term)));
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (isAdmin.HasValue)
            query = query.Where(u => u.IsAdmin == isAdmin.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            .ToListAsync(ct);

        return PaginatedResult<UserProfileDto>.Create(items, total, page, pageSize);
    }

    public async Task<IEnumerable<UserAddressDto>> GetUserAddressesAsync(UserId userId, CancellationToken ct)
    {
        return await context.UserAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new UserAddressDto
            {
                Id = a.Id.Value,
                Title = a.Title,
                ReceiverName = a.ReceiverName,
                PhoneNumber = a.PhoneNumber.Value,
                Province = a.Province,
                City = a.City,
                Address = a.Address,
                PostalCode = a.PostalCode,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                IsDefault = a.IsDefault
            })
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(
        UserId userId,
        Guid? currentSessionId = null,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var current = currentSessionId ?? Guid.Empty;

        return await context.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new UserSessionDto
            {
                Id = s.Id.Value,
                SessionType = string.Empty,
                CreatedByIp = s.IpAddress.Value,
                DeviceInfo = s.DeviceInfo.Value,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                ExpiresAt = s.ExpiresAt,
                IsCurrent = current != Guid.Empty && s.Id.Value == current
            })
            .ToListAsync(ct);
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetUserReviewsPagedAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.ProductReviews
            .AsNoTracking()
            .Where(r => r.UserId == userId && !r.IsDeleted);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id.Value,
                ProductId = r.ProductId.Value,
                UserId = r.UserId.Value,
                UserFullName = r.User.FullName.FirstName + " " + r.User.FullName.LastName,
                Rating = r.Rating.Value,
                Title = r.Title,
                Comment = r.Comment,
                Status = r.Status.Value,
                RejectionReason = r.RejectionReason,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                LikeCount = r.LikeCount,
                DislikeCount = r.DislikeCount,
                AdminReply = r.AdminReply,
                RepliedAt = r.RepliedAt,
                CreatedAt = r.CreatedAt,
                OrderId = r.OrderId
            })
            .ToListAsync(ct);

        return PaginatedResult<ProductReviewDto>.Create(items, total, page, pageSize);
    }
}