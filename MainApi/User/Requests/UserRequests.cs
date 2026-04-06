namespace MainApi.User.Requests;

public record ChangePhoneNumberRequest(string NewPhoneNumber, string OtpCode);

public record ChangeUserRoleRequest(bool IsAdmin);