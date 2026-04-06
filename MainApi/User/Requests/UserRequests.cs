namespace Presentation.User.Requests;

public record ChangePhoneNumberRequest(string NewPhoneNumber, string OtpCode);

public record ChangeUserRoleRequest(bool IsAdmin);