namespace Domain.User.Interfaces;

public interface IUserRepository
{
    Task AddAsync(Aggregates.User user, CancellationToken ct = default);

    void Update(Aggregates.User user);

    Task AddAddressAsync(UserAddress address, CancellationToken ct = default);

    Task<Aggregates.User?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Aggregates.User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default);

    Task<Aggregates.User?> GetWithOtpsAsync(int userId, CancellationToken ct = default);

    Task<Aggregates.User?> GetWithOtpsByPhoneAsync(string phoneNumber, CancellationToken ct = default);

    Task<Aggregates.User?> GetWithOtpsAndSessionsByPhoneAsync(string phoneNumber, CancellationToken ct = default);

    Task<Aggregates.User?> GetWithAddressesAsync(int userId, CancellationToken ct = default);

    Task<Aggregates.User?> GetWithSessionsAsync(int userId, CancellationToken ct = default);

    Task<Aggregates.User?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);

    Task<Aggregates.User?> GetActiveByIdAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, int? excludeUserId = null, CancellationToken ct = default);

    Task<bool> PhoneNumberExistsAsync(string phoneNumber, int userId, CancellationToken ct = default);

    Task<UserAddress?> GetUserAddressAsync(int addressId, CancellationToken ct = default);
}