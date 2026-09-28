using Domain.User.Entities;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

using Infrastructure.Persistence.Repositories;

namespace Infrastructure.User.Repositories;

public sealed class UserRepository(DBContext context)
    : RepositoryBase<Domain.User.Aggregates.User, UserId>(context), IUserRepository
{
    public async Task<Domain.User.Aggregates.User?> GetWithAddressesAsync(UserId id, CancellationToken ct = default)
        => await Context.Users
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<Domain.User.Aggregates.User?> GetActiveByIdAsync(UserId id, CancellationToken ct = default)
        => await Context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);

    public async Task<Domain.User.Aggregates.User?> GetByEmailAsync(Email email, CancellationToken ct = default)
        => await Context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, ct);

    public async Task<Domain.User.Aggregates.User?> GetByPhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken ct = default)
        => await Context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber != null && u.PhoneNumber.Value == phoneNumber.Value, ct);

    public async Task<bool> ExistsByPhoneNumberAsync(
        PhoneNumber phoneNumber, UserId? excludeId = null, CancellationToken ct = default)
    {
        var query = Context.Users
            .IgnoreQueryFilters()
            .Where(u => u.PhoneNumber != null && u.PhoneNumber.Value == phoneNumber.Value);

        if (excludeId is not null)
            query = query.Where(u => u.Id != excludeId);

        return await query.AnyAsync(ct);
    }

    public async Task<UserAddress?> GetUserAddressAsync(UserAddressId addressId, CancellationToken ct = default)
        => await Context.UserAddresses.FirstOrDefaultAsync(a => a.Id == addressId, ct);

    public async Task<IReadOnlyList<Guid>> GetAllActiveUserIdsAsync(CancellationToken ct = default)
        => await Context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .Select(u => u.Id.Value)
            .ToListAsync(ct);
}