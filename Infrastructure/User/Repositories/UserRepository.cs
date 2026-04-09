using Domain.User.Entities;
using Domain.User.Interfaces;
using Infrastructure.Persistence.Context;

namespace Infrastructure.User.Repositories;

public class UserRepository(DBContext context) : IUserRepository
{
    private readonly DBContext _context = context;

    public async Task AddAsync(
        Domain.User.Aggregates.User user,
        CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
    }

    public void Update(Domain.User.Aggregates.User user)
    {
        _context.Users.Update(user);
    }

    public async Task AddAddressAsync(
        UserAddress address,
        CancellationToken ct = default)
    {
        await _context.UserAddresses.AddAsync(address, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetActiveByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetByIdIncludingDeletedAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserAddresses)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetWithAddressesAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserAddresses.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetWithOtpsAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserOtps)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetWithOtpsByPhoneAsync(
        string phoneNumber,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserOtps)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetWithOtpsAndSessionsByPhoneAsync(
        string phoneNumber,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserOtps)
            .Include(u => u.UserSessions.Where(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow))
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetWithSessionsAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.UserSessions.Where(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow))
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
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

    public async Task<bool> PhoneNumberExistsAsync(
        string phoneNumber,
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Users
            .Where(u => u.PhoneNumber == phoneNumber && u.Id != userId)
            .AnyAsync(ct);
    }

    public async Task<UserAddress?> GetUserAddressAsync(
        int addressId,
        CancellationToken ct = default)
    {
        return await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && !a.IsDeleted, ct);
    }
}