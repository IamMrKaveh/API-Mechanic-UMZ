namespace Application.DTOs;

public record LoginRequestDto([Required] string PhoneNumber);

public record VerifyOtpRequestDto([Required] string PhoneNumber, [Required] string Code);

public record RefreshRequestDto(string refreshToken);

public record AuthResponseDto(string Token, UserProfileDto User, DateTime Expires, string RefreshToken);