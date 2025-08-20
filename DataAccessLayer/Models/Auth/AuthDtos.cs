namespace DataAccessLayer.Models.Auth;

public class LoginRequestDto
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyOtpRequestDto
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [Length(6, 6)]
    public string Code { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserProfileDto User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}