namespace Presentation.User.Requests;

public record CreateUserAddressRequest(
    string Title,
    string ReceiverName,
    string PhoneNumber,
    string Province,
    string City,
    string Address,
    string PostalCode,
    decimal? Latitude = null,
    decimal? Longitude = null
);

public record UpdateUserAddressRequest(
    string Title,
    string ReceiverName,
    string PhoneNumber,
    string Province,
    string City,
    string Address,
    string PostalCode,
    bool IsDefault,
    decimal? Latitude = null,
    decimal? Longitude = null
);

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);

public record ChangePhoneNumberRequest(
    string NewPhoneNumber,
    string OtpCode
);

public record ChangeUserRoleRequest(bool IsAdmin);

public record ChangeUserStatusRequest(bool IsActive);