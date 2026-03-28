namespace MainApi.Auth.Requests;

public record LoginRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
}

public record VerifyOtpRequestDto(string PhoneNumber, string Code);

public record RefreshRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}