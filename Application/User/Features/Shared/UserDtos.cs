namespace Application.User.Features.Shared;

public record UserProfileDto
{
    public int Id { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public bool IsAdmin { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public int LoginCount { get; init; }
    public List<UserAddressDto> UserAddresses { get; init; } = new();
}

public record UserAddressDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ReceiverName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
}

public record UserSessionDto
{
    public int Id { get; init; }
    public string SessionType { get; init; } = string.Empty;
    public string CreatedByIp { get; init; } = string.Empty;
    public string? DeviceInfo { get; init; }
    public string? BrowserInfo { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsCurrent { get; init; }
}

public record UserActivityDto
{
    public int Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string IpAddress { get; init; } = string.Empty;
}

public record UserDashboardDto
{
    public UserProfileDto UserProfile { get; init; } = null!;
    public List<OrderDto> RecentOrders { get; init; } = [];
    public List<UserActivityDto> RecentActivities { get; init; } = [];
    public int WishlistCount { get; init; }
    public int OpenTicketsCount { get; init; }
    public int UnreadNotifications { get; init; }
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
}

public record CreateUserAddressDto
{
    public string Title { get; init; } = string.Empty;
    public string ReceiverName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

public record UpdateUserAddressDto
{
    public string Title { get; init; } = string.Empty;
    public string ReceiverName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

public record UpdateProfileDto
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
}

public record ChangeUserStatusDto
{
    public bool IsActive { get; init; }
}

public record ChangePasswordDto
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmNewPassword { get; init; } = string.Empty;
}

public record AdminCreateUserDto
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public bool IsAdmin { get; init; }
}