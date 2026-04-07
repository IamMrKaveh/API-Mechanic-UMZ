namespace Presentation.Auth.Requests;

public record SendOtpRequest(string PhoneNumber);

public record VerifyOtpRequest(
    string PhoneNumber,
    string Code,
    string? DeviceInfo = null
);

public record RefreshRequest(string RefreshToken);

public record RevokeSessionRequest(Guid SessionId);

public record GoogleLoginRequest(string IdToken);