using Domain.User.Entities;
using Domain.User.ValueObjects;

namespace Domain.User.Interfaces;

public interface IUserRepository
{
    Task AddAsync(Aggregates.User user, CancellationToken ct = default);

    void Update(Aggregates.User user);

    Task<Aggregates.User?> GetByIdAsync(UserId id, CancellationToken ct = default);

    Task<Aggregates.User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default);

    Task<Aggregates.User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<Aggregates.User?> GetWithAddressesAsync(UserId id, CancellationToken ct = default);

    Task<Aggregates.User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken ct = default);

    Task<Aggregates.User?> GetActiveByIdAsync(UserId id, CancellationToken ct = default);

    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, UserId? excludeUserId = null, CancellationToken ct = default);

    Task<bool> ExistsByEmailAsync(string email, UserId? excludeUserId = null, CancellationToken ct = default);

    Task<UserAddress?> GetUserAddressAsync(UserAddressId addressId, CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.User>> GetAllActiveAsync(CancellationToken ct = default);

    Task<bool> ExistsAsync(UserId id, CancellationToken ct = default);
}