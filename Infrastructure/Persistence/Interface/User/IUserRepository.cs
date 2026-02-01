namespace Infrastructure.Persistence.Interface.User;

public interface IUserRepository
{
    Task<Domain.User.User?> GetUserByPhoneNumberAsync(string phoneNumber, bool ignoreQueryFilters = false);

    Task<Domain.User.User?> GetUserByIdAsync(int id, bool ignoreQueryFilters = false);

    Task<UserAddress?> GetUserAddressAsync(int userAddressId);
    Task AddAddressAsync(UserAddress address);
    void UpdateUserAddress(UserAddress address);
    void DeleteUserAddress(UserAddress address);

    Task AddUserAsync(Domain.User.User user);

    void UpdateUser(Domain.User.User user);

    Task<bool> PhoneNumberExistsAsync(string phoneNumber);

    Task<UserOtp?> GetActiveOtpAsync(int userId);

    Task AddOtpAsync(UserOtp otp);

    Task InvalidateOtpsAsync(int userId);

    Task<UserSession?> GetActiveSessionByTokenAsync(string refreshToken);

    Task AddSessionAsync(UserSession session);

    Task RevokeSessionAsync(UserSession session, string? newRefreshTokenHash = null);

    Task RevokeAllUserSessionsAsync(int userId);

    Task<(IEnumerable<Domain.User.User> Users, int TotalCount)> GetUsersAsync(bool includeDeleted, int page, int pageSize);
}