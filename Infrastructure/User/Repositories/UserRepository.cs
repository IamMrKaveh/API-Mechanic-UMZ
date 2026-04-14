using Domain.User.Entities;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Infrastructure.User.Repositories;

public sealed class UserRepository(DBContext context) : IUserRepository
{
    public async Task<Domain.User.Aggregates.User?> GetByIdAsync(UserId id, CancellationToken ct = default)
        => await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<Domain.User.Aggregates.User?> GetWithAddressesAsync(UserId id, CancellationToken ct = default)
        => await context.Users
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<Domain.User.Aggregates.User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken ct = default)
        => await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<Domain.User.Aggregates.User?> GetActiveByIdAsync(UserId id, CancellationToken ct = default)
        => await context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);

    public async Task<Domain.User.Aggregates.User?> GetByEmailAsync(Email email, CancellationToken ct = default)
        => await context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, ct);

    public async Task<Domain.User.Aggregates.User?> GetByPhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken ct = default)
        => await context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber != null && u.PhoneNumber.Value == phoneNumber.Value, ct);

    public async Task<bool> ExistsByPhoneNumberAsync(
        PhoneNumber phoneNumber, UserId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.Users
            .IgnoreQueryFilters()
            .Where(u => u.PhoneNumber != null && u.PhoneNumber.Value == phoneNumber.Value);

        if (excludeId is not null)
            query = query.Where(u => u.Id != excludeId);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsByEmailAsync(
        Email email, UserId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Email.Value == email.Value);

        if (excludeId is not null)
            query = query.Where(u => u.Id != excludeId);

        return await query.AnyAsync(ct);
    }

    public async Task<UserAddress?> GetUserAddressAsync(UserAddressId addressId, CancellationToken ct = default)
        => await context.UserAddresses.FirstOrDefaultAsync(a => a.Id == addressId, ct);

    public async Task<IReadOnlyList<Domain.User.Aggregates.User>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var result = await context.Users
            .Where(u => u.IsActive)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<bool> ExistsAsync(UserId id, CancellationToken ct = default)
        => await context.Users.AnyAsync(u => u.Id == id, ct);

    public async Task AddAsync(Domain.User.Aggregates.User user, CancellationToken ct = default)
        => await context.Users.AddAsync(user, ct);

    public void Update(Domain.User.Aggregates.User user)
        => context.Users.Update(user);

    public void SetOriginalRowVersion(Domain.User.Aggregates.User user, byte[] rowVersion)
        => context.Entry(user).Property("RowVersion").OriginalValue = rowVersion;
}