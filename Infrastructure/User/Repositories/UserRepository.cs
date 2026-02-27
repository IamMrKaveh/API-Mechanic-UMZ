namespace Infrastructure.User.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DBContext _context;

    public UserRepository(DBContext context)
    {
        _context = context;
    }

    public async Task<Domain.User.User?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<Domain.User.User?> GetByIdIncludingDeletedAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserAddresses)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<Domain.User.User?> GetByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
    }

    public async Task<Domain.User.User?> GetWithAddressesAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<Domain.User.User?> GetWithOtpsAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserOtps)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<Domain.User.User?> GetWithSessionsAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserSessions.Where(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow))
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<bool> PhoneNumberExistsAsync(
        string phoneNumber,
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Where(u => u.PhoneNumber == phoneNumber && u.Id != userId)
            .AnyAsync(ct);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(
        string phoneNumber,
        int? excludeUserId = null,
        CancellationToken ct = default)
    {
        var query = _context.Users.Where(u => u.PhoneNumber == phoneNumber);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(
        Domain.User.User user,
        CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
    }

    public void Update(Domain.User.User user)
    {
        _context.Users.Update(user);
    }

    public void UpdateUser(Domain.User.User user)
    {
        _context.Users.Update(user);
    }

    public async Task<bool> HasActiveOrdersAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Orders
            .AnyAsync(o => o.UserId == userId && !o.IsDeleted && !o.IsPaid, ct);
    }

    public void SetOriginalRowVersion(Domain.User.User user, byte[] rowVersion)
    {
        _context.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<UserAddress?> GetUserAddressAsync(int addressId, CancellationToken ct = default)
    {
        return await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && !a.IsDeleted, ct);
    }

    public async Task AddAddressAsync(UserAddress address, CancellationToken ct = default)
    {
        await _context.UserAddresses.AddAsync(address, ct);
    }

    public async Task DeleteUserOtpsAsync(int userId)
    {
        var otps = await _context.UserOtps.Where(o => o.UserId == userId).ToListAsync();
        _context.UserOtps.RemoveRange(otps);
    }

    public async Task DeleteUserOtpsAsync(int userId, CancellationToken ct = default)
    {
        var otps = await _context.UserOtps.Where(o => o.UserId == userId).ToListAsync(ct);
        _context.UserOtps.RemoveRange(otps);
    }

    public async Task AddUserOtpAsync(UserOtp userOtp)
    {
        await _context.UserOtps.AddAsync(userOtp);
    }

    public async Task AddUserOtpAsync(UserOtp otp, CancellationToken ct = default)
    {
        await _context.UserOtps.AddAsync(otp, ct);
    }

    public async Task<UserOtp?> GetActiveOtpAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.UserOtps
            .AsNoTracking()
            .Where(o => o.UserId == userId && !o.IsUsed && o.ExpiresAt >= DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddSessionAsync(UserSession session, CancellationToken ct = default)
    {
        await _context.UserSessions.AddAsync(session, ct);
    }

    public async Task<UserSession?> GetSessionBySelectorAsync(string tokenSelector, CancellationToken ct = default)
    {
        return await _context.UserSessions
            .FirstOrDefaultAsync(s => s.TokenSelector == tokenSelector, ct);
    }

    public async Task RevokeSessionAsync(int sessionId, CancellationToken ct = default)
    {
        var session = await _context.UserSessions.FindAsync(new object[] { sessionId }, ct);
        session?.Revoke();
    }

    public async Task RevokeAllUserSessionsAsync(int userId, CancellationToken ct = default)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var s in sessions)
            s.Revoke();
    }

    public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(int userId, CancellationToken ct = default)
    {
        return await _context.UserSessions
            .AsNoTracking()
            .Where(s =>
                s.UserId == userId &&
                s.RevokedAt == null &&
                s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<Domain.User.User> Users, int TotalCount)> GetPagedAsync(
        string? search, bool? isActive, bool? isAdmin, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

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
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .ToListAsync(ct);

        return (users, totalCount);
    }

    public async Task<(object users, int totalItems)> GetUsersAsync(bool includeDeleted, int page, int pageSize)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!includeDeleted)
            query = query.Where(u => !u.IsDeleted);

        var totalItems = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .ToListAsync();

        return (users, totalItems);
    }

    public async Task<bool> IsInWishlistAsync(int userId, int productId, CancellationToken ct = default)
    {
        return await _context.Wishlists
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId, ct);
    }

    public async Task AddToWishlistAsync(int userId, int productId, CancellationToken ct = default)
    {
        var wishlist = Wishlist.Create(userId, productId);
        await _context.Wishlists.AddAsync(wishlist, ct);
    }

    public async Task RemoveFromWishlistAsync(int userId, int productId, CancellationToken ct = default)
    {
        var wishlist = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);

        if (wishlist != null)
            _context.Wishlists.Remove(wishlist);
    }

    public IQueryable<Domain.User.User> GetUsersQuery(bool includeDeleted)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!includeDeleted)
            query = query.Where(u => !u.IsDeleted);

        return query;
    }
}