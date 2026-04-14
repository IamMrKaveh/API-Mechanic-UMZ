using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.User.Repositories;

public sealed class UserRepository(DBContext context) : IUserRepository
{
    public async Task<Domain.User.Aggregates.User?> GetByIdAsync(UserId userId, CancellationToken ct = default)
    {
        return await context.Users
            .Include(u => u.UserAddresses)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetByPhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken ct = default)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
    }

    public async Task<Domain.User.Aggregates.User?> GetByEmailAsync(Email email, CancellationToken ct = default)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<bool> ExistsByPhoneAsync(string phoneNumber, UserId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.Users.Where(u => u.PhoneNumber == phoneNumber);
        if (excludeId is not null)
            query = query.Where(u => u.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, UserId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.Users.Where(u => u.Email == email);
        if (excludeId is not null)
            query = query.Where(u => u.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Domain.User.Aggregates.User user, CancellationToken ct = default)
    {
        await context.Users.AddAsync(user, ct);
    }

    public void Update(Domain.User.Aggregates.User user)
    {
        context.Users.Update(user);
    }

    public void SetOriginalRowVersion(Domain.User.Aggregates.User user, byte[] rowVersion)
    {
        context.Entry(user).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }
}