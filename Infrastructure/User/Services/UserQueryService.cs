namespace Infrastructure.User.Services;

public class UserQueryService : IUserQueryService
{
    private readonly LedkaContext _context;

    public UserQueryService(LedkaContext context)
    {
        _context = context;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                IsAdmin = u.IsAdmin,
                CreatedAt = u.CreatedAt,
                UserAddresses = u.UserAddresses
                    .Where(a => !a.IsDeleted)
                    .Select(a => new UserAddressDto
                    {
                        Id = a.Id,
                        Title = a.Title,
                        ReceiverName = a.ReceiverName,
                        PhoneNumber = a.PhoneNumber,
                        Province = a.Province,
                        City = a.City,
                        Address = a.Address,
                        PostalCode = a.PostalCode,
                        IsDefault = a.IsDefault
                    }).ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PaginatedResult<UserProfileDto>> GetUsersPagedAsync(
        string? search, bool? isActive, bool? isAdmin, bool includeDeleted, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!includeDeleted)
            query = query.Where(u => !u.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (isAdmin.HasValue)
            query = query.Where(u => u.IsAdmin == isAdmin.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(u =>
                u.PhoneNumber.Contains(searchTerm) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                IsAdmin = u.IsAdmin,
                CreatedAt = u.CreatedAt,
                UserAddresses = u.UserAddresses
                    .Where(a => !a.IsDeleted)
                    .Select(a => new UserAddressDto
                    {
                        Id = a.Id,
                        Title = a.Title,
                        ReceiverName = a.ReceiverName,
                        PhoneNumber = a.PhoneNumber,
                        Province = a.Province,
                        City = a.City,
                        Address = a.Address,
                        PostalCode = a.PostalCode,
                        IsDefault = a.IsDefault
                    }).ToList()
            })
            .ToListAsync(ct);

        return PaginatedResult<UserProfileDto>.Create(users, totalCount, page, pageSize);
    }

    public async Task<IEnumerable<UserAddressDto>> GetUserAddressesAsync(
        int userId, CancellationToken ct = default)
    {
        return await _context.UserAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new UserAddressDto
            {
                Id = a.Id,
                Title = a.Title,
                ReceiverName = a.ReceiverName,
                PhoneNumber = a.PhoneNumber,
                Province = a.Province,
                City = a.City,
                Address = a.Address,
                PostalCode = a.PostalCode,
                IsDefault = a.IsDefault
            })
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(
        int userId, CancellationToken ct = default)
    {
        return await _context.UserSessions
            .AsNoTracking()
            .Where(s =>
                s.UserId == userId &&
                s.RevokedAt == null &&
                s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
            .Select(s => new UserSessionDto
            {
                Id = s.Id,
                SessionType = s.SessionType ?? "Web",
                CreatedByIp = s.CreatedByIp,
                DeviceInfo = UserAgentHelper.GetDeviceInfo(s.UserAgent),
                BrowserInfo = UserAgentHelper.GetBrowserInfo(s.UserAgent),
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                ExpiresAt = s.ExpiresAt,
                IsCurrent = false
            })
            .ToListAsync(ct);
    }

    public async Task<UserDashboardDto?> GetUserDashboardAsync(
        int userId, CancellationToken ct = default)
    {
        var user = await GetUserProfileAsync(userId, ct);
        if (user == null) return null;

        var totalOrders = await _context.Orders
            .CountAsync(o => o.UserId == userId && !o.IsDeleted, ct);

        var totalSpent = await _context.Orders
            .Where(o => o.UserId == userId && o.PaymentDate != null && !o.IsDeleted)
            .SumAsync(o => o.FinalAmount.Amount, ct);

        var recentOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                TotalAmount = o.TotalAmount.Amount,
                FinalAmount = o.FinalAmount.Amount,
                CreatedAt = o.CreatedAt,
                IsPaid = o.PaymentDate != null
            })
            .ToListAsync(ct);

        var wishlistCount = await _context.Wishlists
            .CountAsync(w => w.UserId == userId, ct);

        var openTicketsCount = await _context.Tickets
            .CountAsync(t => t.UserId == userId && t.Status != "Closed", ct);

        var unreadNotifications = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

        return new UserDashboardDto
        {
            UserProfile = user,
            RecentOrders = recentOrders,
            TotalOrders = totalOrders,
            TotalSpent = totalSpent,
            WishlistCount = wishlistCount,
            OpenTicketsCount = openTicketsCount,
            UnreadNotifications = unreadNotifications
        };
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetUserReviewsPagedAsync(
        int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ProductReviews
            .AsNoTracking()
            .Where(r => r.UserId == userId && !r.IsDeleted);

        var totalCount = await query.CountAsync(ct);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                Status = r.Status,
                AdminReply = r.AdminReply,
                CreatedAt = r.CreatedAt,
                UserName = r.User != null
                    ? (r.User.FirstName + " " + r.User.LastName).Trim()
                    : "کاربر ناشناس"
            })
            .ToListAsync(ct);

        return PaginatedResult<ProductReviewDto>.Create(reviews, totalCount, page, pageSize);
    }

    public async Task<PaginatedResult<WishlistItemDto>> GetUserWishlistPagedAsync(
        int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Wishlists
            .AsNoTracking()
            .Where(w => w.UserId == userId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WishlistItemDto
            {
                Id = w.Id,
                ProductId = w.ProductId,
                ProductName = w.Product.Name.Value,
                MinPrice = w.Product.Stats.MinPrice.Amount,
                IsInStock = w.Product.Stats.TotalStock > 0 || w.Product.Variants.Any(v => v.IsUnlimited),
                IconUrl = null,
                AddedAt = w.CreatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<WishlistItemDto>.Create(items, totalCount, page, pageSize);
    }
}