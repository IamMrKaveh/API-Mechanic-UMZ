namespace Application.Common.Interfaces.Persistence;

public interface IUserRepository
{
    Task<Domain.User.User?> GetUserByPhoneNumberAsync(string phoneNumber, bool ignoreQueryFilters = false);

    Task<Domain.User.User?> GetUserByIdAsync(int id, bool ignoreQueryFilters = false);

    Task<Domain.User.UserAddress?> GetUserAddressAsync(int userAddressId);
    void UpdateUserAddress(UserAddress address);
    void DeleteUserAddress(UserAddress address);

    Task AddUserAsync(Domain.User.User user);

    void UpdateUser(Domain.User.User user);

    Task<bool> PhoneNumberExistsAsync(string phoneNumber);

    Task<Domain.User.UserOtp?> GetActiveOtpAsync(int userId);

    Task AddOtpAsync(Domain.User.UserOtp otp);

    Task InvalidateOtpsAsync(int userId);

    Task<Domain.Auth.UserSession?> GetActiveSessionByTokenAsync(string refreshToken);

    Task AddSessionAsync(Domain.Auth.UserSession session);

    Task RevokeSessionAsync(Domain.Auth.UserSession session, string? newRefreshTokenHash = null);

    Task RevokeAllUserSessionsAsync(int userId);

    Task<IEnumerable<Domain.User.User>> GetUsersAsync(bool includeDeleted);
}