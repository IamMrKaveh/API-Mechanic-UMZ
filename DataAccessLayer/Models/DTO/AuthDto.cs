namespace DataAccessLayer.Models.DTO;

public class LoginRequestDto
{
    [Required(ErrorMessage = "شماره تلفن الزامی است")]
    [Phone(ErrorMessage = "فرمت شماره تلفن نامعتبر است")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "فرمت شماره تلفن ایرانی نامعتبر است")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyOtpRequestDto
{
    [Required(ErrorMessage = "شماره تلفن الزامی است")]
    [Phone(ErrorMessage = "فرمت شماره تلفن نامعتبر است")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "فرمت شماره تلفن ایرانی نامعتبر است")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "کد تایید الزامی است")]
    [StringLength(4, MinimumLength = 4, ErrorMessage = "کد تایید باید 4 رقم باشد")]
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
    public List<UserAddressDto> Addresses { get; set; } = new();
}

public class UpdateProfileDto
{
    [MaxLength(100, ErrorMessage = "نام نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string? FirstName { get; set; }

    [MaxLength(100, ErrorMessage = "نام خانوادگی نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string? LastName { get; set; }
}

public class RefreshRequestDto
{
    [Required(ErrorMessage = "توکن الزامی است")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class UserAddressDto
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required, Phone, MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(10)]
    [RegularExpression(@"^\d{10}$")]
    public string PostalCode { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}