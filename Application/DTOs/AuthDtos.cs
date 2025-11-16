namespace Application.DTOs;

public record LoginRequestDto([Required] string PhoneNumber);

public record VerifyOtpRequestDto([Required] string PhoneNumber, [Required] string Code);

public record RefreshRequestDto(string refreshToken);

public record AuthResponseDto(string Token, UserProfileDto User, DateTime Expires, string RefreshToken);

public class UpdateProfileDto
{
    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ICollection<UserAddressDto> UserAddresses { get; set; } = [];
}

public class UserAddressDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}


public class CreateUserAddressDto
{
    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string ReceiverName { get; set; } = string.Empty;
    [Required, StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string Province { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string City { get; set; } = string.Empty;
    [Required, StringLength(500)]
    public string Address { get; set; } = string.Empty;
    [Required, StringLength(10)]
    public string PostalCode { get; set; } = string.Empty;
}

public class UpdateUserAddressDto
{
    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string ReceiverName { get; set; } = string.Empty;
    [Required, StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string Province { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string City { get; set; } = string.Empty;
    [Required, StringLength(500)]
    public string Address { get; set; } = string.Empty;
    [Required, StringLength(10)]
    public string PostalCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}


public class ChangeUserStatusDto
{
    public bool IsActive { get; set; }
}