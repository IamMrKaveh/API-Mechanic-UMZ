namespace DataAccessLayer.Models.DTO;

public class LoginRequestDto
{
    [Required]
    [Phone]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Invalid Iranian phone number format.")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyOtpRequestDto
{
    [Required]
    [Phone]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Invalid Iranian phone number format.")]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    [StringLength(4, MinimumLength = 4)]
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
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsAdmin { get; set; }
}

public class UpdateProfileDto
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }
}

public class RefreshRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}