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

    public async Task<PaginatedResult<AdminUserListItemDto>> GetAdminUsersPagedAsync(
        AdminUserFilterParams filter,
        CancellationToken ct = default)
    {
        var query = context.Users.AsNoTracking();

        if (!filter.IncludeDeleted)
            query = query.Where(u => u.IsActive || u.IsAdmin || u.CreatedAt != DateTime.MinValue);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.Value.ToLower().Contains(term) ||
                u.FullName.FirstName.ToLower().Contains(term) ||
                u.FullName.LastName.ToLower().Contains(term) ||
                (u.PhoneNumber != null && u.PhoneNumber.Value.Contains(term)));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        if (filter.IsAdmin.HasValue)
            query = query.Where(u => u.IsAdmin == filter.IsAdmin.Value);

        if (filter.CreatedAfter.HasValue)
            query = query.Where(u => u.CreatedAt >= filter.CreatedAfter.Value);

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            var role = filter.Role.Trim();
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.IsAdmin);
            else if (string.Equals(role, "User", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => !u.IsAdmin);
        }

        var total = await query.CountAsync(ct);

        var projected = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new
            {
                Id = u.Id,
                IdValue = u.Id.Value,
                FirstName = u.FullName.FirstName,
                LastName = u.FullName.LastName,
                Email = u.Email.Value,
                PhoneNumber = u.PhoneNumber != null ? u.PhoneNumber.Value : "",
                u.IsActive,
                u.IsAdmin,
                u.IsEmailVerified,
                u.CreatedAt,
                u.UpdatedAt,
                u.LastLoginAt,
                IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTime.UtcNow
            })
            .ToListAsync(ct);

        var userIds = projected.Select(p => p.Id).ToList();

        var orderStats = await context.Orders
            .AsNoTracking()
            .Where(o => userIds.Contains(o.UserId))
            .GroupBy(o => o.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                OrderCount = g.Count(),
                CompletedOrderCount = g.Count(o => o.Status == Domain.Order.ValueObjects.OrderStatusValue.Delivered),
                TotalSpent = g
                    .Where(o => o.Status == Domain.Order.ValueObjects.OrderStatusValue.Delivered)
                    .Sum(o => (decimal?)o.FinalAmount.Amount) ?? 0m
            })
            .ToDictionaryAsync(x => x.UserId, ct);

        var walletBalances = await context.Wallets
            .AsNoTracking()
            .Where(w => userIds.Contains(w.OwnerId))
            .Select(w => new { w.OwnerId, Balance = (decimal?)w.Balance.Amount ?? 0m })
            .ToDictionaryAsync(x => x.OwnerId, x => x.Balance, ct);

        var addressStats = await context.UserAddresses
            .AsNoTracking()
            .Where(a => userIds.Contains(a.UserId))
            .GroupBy(a => a.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Count = g.Count(),
                Default = g.Where(a => a.IsDefault)
                    .Select(a => a.Province + " - " + a.City)
                    .FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.UserId, ct);

        var openTickets = await context.Tickets
            .AsNoTracking()
            .Where(t => userIds.Contains(t.CustomerId))
            .GroupBy(t => t.CustomerId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var items = projected.Select(p =>
        {
            orderStats.TryGetValue(p.Id, out var ord);
            walletBalances.TryGetValue(p.Id, out var wallet);
            addressStats.TryGetValue(p.Id, out var addr);
            openTickets.TryGetValue(p.Id, out var tk);

            return new AdminUserListItemDto
            {
                Id = p.IdValue,
                FullName = $"{p.FirstName} {p.LastName}".Trim(),
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                PhoneNumber = p.PhoneNumber,
                IsActive = p.IsActive,
                IsAdmin = p.IsAdmin,
                IsEmailVerified = p.IsEmailVerified,
                IsLockedOut = p.IsLockedOut,
                IsDeleted = false,
                Roles = p.IsAdmin ? new List<string> { "Admin" } : new List<string> { "User" },
                OrderCount = ord?.OrderCount ?? 0,
                CompletedOrderCount = ord?.CompletedOrderCount ?? 0,
                TotalSpent = ord?.TotalSpent ?? 0m,
                DefaultAddressSummary = addr?.Default,
                AddressCount = addr?.Count ?? 0,
                WalletBalance = wallet,
                OpenTicketsCount = tk,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                LastLoginAt = p.LastLoginAt
            };
        }).ToList();

        if (filter.MinTotalSpent.HasValue)
            items = items.Where(i => i.TotalSpent >= filter.MinTotalSpent.Value).ToList();

        return PaginatedResult<AdminUserListItemDto>.Create(items, total, filter.Page, filter.PageSize);
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
        const long expiringThresholdSeconds = 24 * 60 * 60;

        var raw = await context.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                Id = s.Id.Value,
                Ip = s.IpAddress.Value,
                Device = s.DeviceInfo.Value,
                s.CreatedAt,
                s.LastActivityAt,
                s.ExpiresAt
            })
            .ToListAsync(ct);

        return raw.Select(s =>
        {
            var totalSeconds = (long)Math.Max(0, (s.ExpiresAt - now).TotalSeconds);
            return new UserSessionDto
            {
                Id = s.Id,
                CreatedByIp = s.Ip,
                DeviceInfo = ExtractDeviceLabel(s.Device),
                BrowserInfo = ExtractBrowserLabel(s.Device),
                PlatformInfo = ExtractPlatformLabel(s.Device),
                SessionType = ExtractSessionType(s.Device),
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt ?? s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                IsCurrent = current != Guid.Empty && s.Id == current,
                RemainingSeconds = totalSeconds,
                IsExpiringSoon = totalSeconds > 0 && totalSeconds <= expiringThresholdSeconds
            };
        });
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetUserReviewsPagedAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var safePage = page <= 0 ? 1 : page;
        var safeSize = pageSize <= 0 ? 10 : pageSize;

        var query = context.ProductReviews
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId && !r.IsDeleted);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(r => new
            {
                r.Id,
                r.ProductId,
                r.UserId,
                FirstName = r.User != null && r.User.FullName != null ? r.User.FullName.FirstName : null,
                LastName = r.User != null && r.User.FullName != null ? r.User.FullName.LastName : null,
                Rating = r.Rating.Value,
                r.Title,
                r.Comment,
                Status = r.Status.Value,
                r.RejectionReason,
                r.AdminReply,
                r.RepliedAt,
                r.IsVerifiedPurchase,
                r.LikeCount,
                r.DislikeCount,
                r.CreatedAt,
                r.OrderId
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new ProductReviewDto
        {
            Id = r.Id.Value,
            ProductId = r.ProductId.Value,
            UserId = r.UserId.Value,
            UserFullName = BuildUserFullName(r.FirstName, r.LastName),
            Rating = r.Rating,
            Title = r.Title,
            Comment = r.Comment,
            Status = r.Status,
            RejectionReason = r.RejectionReason,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            LikeCount = r.LikeCount,
            DislikeCount = r.DislikeCount,
            AdminReply = r.AdminReply,
            RepliedAt = r.RepliedAt,
            CreatedAt = r.CreatedAt,
            OrderId = r.OrderId?.Value
        }).ToList();

        return PaginatedResult<ProductReviewDto>.Create(items, total, safePage, safeSize);
    }

    private static string BuildUserFullName(string? firstName, string? lastName)
    {
        var first = (firstName ?? string.Empty).Trim();
        var last = (lastName ?? string.Empty).Trim();
        var full = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(full) ? "کاربر حذف‌شده" : full;
    }

    private static string ExtractDeviceLabel(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent) ||
            userAgent.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return "دستگاه ناشناس";

        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("iphone")) return "iPhone";
        if (ua.Contains("ipad")) return "iPad";
        if (ua.Contains("android"))
        {
            var idx = ua.IndexOf("android", StringComparison.Ordinal);
            var tail = userAgent[idx..];
            var semi = tail.IndexOf(';');
            return semi > 0 ? tail[..semi].Trim() : "Android";
        }
        if (ua.Contains("windows")) return "کامپیوتر ویندوز";
        if (ua.Contains("macintosh") || ua.Contains("mac os x")) return "مک";
        if (ua.Contains("linux")) return "کامپیوتر لینوکس";

        return userAgent.Length > 60 ? userAgent[..60] : userAgent;
    }

    private static string ExtractBrowserLabel(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return string.Empty;
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("edg/")) return "Microsoft Edge";
        if (ua.Contains("opr/") || ua.Contains("opera")) return "Opera";
        if (ua.Contains("firefox")) return "Firefox";
        if (ua.Contains("chrome") && !ua.Contains("edg") && !ua.Contains("opr")) return "Chrome";
        if (ua.Contains("safari") && !ua.Contains("chrome")) return "Safari";
        return "مرورگر ناشناس";
    }

    private static string ExtractPlatformLabel(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return string.Empty;
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("windows nt 10")) return "Windows 10/11";
        if (ua.Contains("windows nt")) return "Windows";
        if (ua.Contains("mac os x") || ua.Contains("macintosh")) return "macOS";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("ios")) return "iOS";
        if (ua.Contains("linux")) return "Linux";
        return string.Empty;
    }

    private static string ExtractSessionType(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "وب";
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("mobile") || ua.Contains("iphone") || ua.Contains("android")) return "موبایل";
        if (ua.Contains("ipad") || ua.Contains("tablet")) return "تبلت";
        return "وب دسکتاپ";
    }
}
