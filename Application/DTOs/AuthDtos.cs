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
    public required string PhoneNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<UserAddressDto> Addresses { get; set; } = [];
}

public class UserAddressDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string ReceiverName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Province { get; set; }
    public required string City { get; set; }
    public required string Address { get; set; }
    public required string PostalCode { get; set; }
    public bool IsDefault { get; set; }
}

public class ChangeUserStatusDto
{
    public bool IsActive { get; set; }
}