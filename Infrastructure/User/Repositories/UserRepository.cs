namespace Infrastructure.User.Repositories;

public class UserRepository : IUserRepository
{
    private readonly LedkaContext _context;

    public UserRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<Domain.User.User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<Domain.User.User?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserAddresses)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<Domain.User.User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
    }

    public async Task<Domain.User.User?> GetWithAddressesAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<Domain.User.User?> GetWithOtpsAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserOtps)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<Domain.User.User?> GetForAuthenticationAsync(string phoneNumber, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserOtps)
            .Include(u => u.UserSessions.Where(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow))
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
    }

    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber, int? excludeUserId = null, CancellationToken ct = default)
    {
        var query = _context.Users.Where(u => u.PhoneNumber == phoneNumber);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default)
    {
        var query = _context.Users.Where(u => u.Email == email);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Domain.User.User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
    }

    public void Update(Domain.User.User user)
    {
        _context.Users.Update(user);
    }

    public async Task<bool> HasActiveOrdersAsync(int userId, CancellationToken ct = default)
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

    public async Task AddUserOtpAsync(UserOtp userOtp)
    {
        await _context.UserOtps.AddAsync(userOtp);
    }

    public async Task<UserOtp?> GetActiveOtpAsync(int userId)
    {
        return await _context.UserOtps
            .Where(o => o.UserId == userId && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddSessionAsync(UserSession session)
    {
        await _context.UserSessions.AddAsync(session);
    }

    public async Task<UserSession?> GetSessionBySelectorAsync(string selector)
    {
        return await _context.UserSessions.FirstOrDefaultAsync(s => s.TokenSelector == selector);
    }

    public async Task RevokeSessionAsync(int sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        session?.Revoke();
    }

    public async Task RevokeAllUserSessionsAsync(int userId)
    {
        var sessions = await _context.UserSessions.Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync();
        foreach (var s in sessions)
            s.Revoke();
    }

    public Task<Domain.User.User?> GetWithSessionsAsync(int userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<Domain.User.User> Users, int TotalCount)> GetPagedAsync(string? search, bool? isActive, bool? isAdmin, int page, int pageSize, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, int? excludeUserId = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<UserOtp?> GetActiveOtpAsync(int userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddUserOtpAsync(UserOtp otp, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteUserOtpsAsync(int userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<UserSession?> GetSessionBySelectorAsync(string tokenSelector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddSessionAsync(UserSession session, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task RevokeSessionAsync(int sessionId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task RevokeAllUserSessionsAsync(int userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<UserSession>> GetActiveSessionsAsync(int userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsInWishlistAsync(int userId, int productId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddToWishlistAsync(int userId, int productId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task RemoveFromWishlistAsync(int userId, int productId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<(object users, int totalItems)> GetUsersAsync(bool includeDeleted, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public void UpdateUser(Domain.User.User user)
    {
        throw new NotImplementedException();
    }

    public Task AddUserAsync(Domain.User.User user)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PhoneNumberExistsAsync(string phoneNumber, int userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}