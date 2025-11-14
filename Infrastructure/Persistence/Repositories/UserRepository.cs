namespace Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly LedkaContext _context;

    public UserRepository(LedkaContext context)
    {
        _context = context;
    }

    public Task<Domain.User.User?> GetUserByPhoneNumberAsync(string phoneNumber, bool ignoreQueryFilters = false)
    {
        var query = _context.Set<Domain.User.User>().AsQueryable();
        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }
        return query.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public Task<Domain.User.User?> GetUserByIdAsync(int id, bool ignoreQueryFilters = false)
    {
        var query = _context.Set<Domain.User.User>().AsQueryable();
        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }
        return query.Include(u => u.UserAddresses).FirstOrDefaultAsync(u => u.Id == id);
    }
    public Task<Domain.User.UserAddress?> GetUserAddressAsync(int userAddressId)
    {
        return _context.Set<Domain.User.UserAddress>().FindAsync(userAddressId).AsTask();
    }


    public async Task AddUserAsync(Domain.User.User user)
    {
        await _context.Set<Domain.User.User>().AddAsync(user);
    }

    public void UpdateUser(Domain.User.User user)
    {
        _context.Set<Domain.User.User>().Update(user);
    }

    public Task<bool> PhoneNumberExistsAsync(string phoneNumber)
    {
        return _context.Set<Domain.User.User>().AnyAsync(u => u.PhoneNumber == phoneNumber);
    }

    public Task<Domain.User.UserOtp?> GetActiveOtpAsync(int userId)
    {
        return _context.Set<Domain.User.UserOtp>()
            .Where(o => o.UserId == userId && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddOtpAsync(Domain.User.UserOtp otp)
    {
        await _context.Set<Domain.User.UserOtp>().AddAsync(otp);
    }

    public async Task InvalidateOtpsAsync(int userId)
    {
        await _context.Set<Domain.User.UserOtp>()
            .Where(o => o.UserId == userId && (o.ExpiresAt <= DateTime.UtcNow || o.IsUsed))
            .ExecuteDeleteAsync();
    }

    public async Task<Domain.Auth.UserSession?> GetActiveSessionByTokenAsync(string refreshToken)
    {
        var parts = refreshToken?.Split(':');
        if (parts == null || parts.Length != 2)
        {
            return null;
        }

        var selector = parts[0];
        var verifier = parts[1];

        var session = await _context.Set<Domain.Auth.UserSession>()
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenSelector == selector &&
                                        t.RevokedAt == null &&
                                        t.ExpiresAt > DateTime.UtcNow);

        if (session == null)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(verifier, session.TokenVerifierHash))
        {
            return null;
        }

        return session;
    }

    public async Task AddSessionAsync(Domain.Auth.UserSession session)
    {
        await _context.Set<Domain.Auth.UserSession>().AddAsync(session);
    }

    public async Task RevokeSessionAsync(Domain.Auth.UserSession session, string? newRefreshTokenHash = null)
    {
        session.RevokedAt = DateTime.UtcNow;
        session.ReplacedByTokenHash = newRefreshTokenHash;

        if (!string.IsNullOrEmpty(session.ReplacedByTokenHash))
        {
            var nextSession = await _context.Set<Domain.Auth.UserSession>().FirstOrDefaultAsync(t => t.TokenVerifierHash == session.ReplacedByTokenHash);
            if (nextSession != null)
            {
                await RevokeSessionAsync(nextSession);
            }
        }
    }

    public async Task RevokeAllUserSessionsAsync(int userId)
    {
        await _context.Set<Domain.Auth.UserSession>()
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.RevokedAt, DateTime.UtcNow));
    }

    public async Task<IEnumerable<Domain.User.User>> GetUsersAsync(bool includeDeleted)
    {
        var query = _context.Set<Domain.User.User>().AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.ToListAsync();
    }
}