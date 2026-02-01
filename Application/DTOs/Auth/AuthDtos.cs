namespace Application.DTOs.Auth;

public record LoginRequestDto(string PhoneNumber);

public record VerifyOtpRequestDto(string PhoneNumber, string Code);

public record RefreshRequestDto(string refreshToken);

public record AuthResponseDto(string Token, UserProfileDto User, DateTime Expires, string RefreshToken);