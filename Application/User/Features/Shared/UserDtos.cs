namespace Application.User.Features.Shared;

public class UserProfileDto
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int LoginCount { get; set; }
    public List<UserAddressDto> UserAddresses { get; set; } = new();
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
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class UserActivityDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty; public string Details { get; set; } = string.Empty; public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

public class UserDashboardDto
{
    public UserProfileDto UserProfile { get; set; } = null!; public List<OrderDto> RecentOrders { get; set; } = []; public List<UserActivityDto> RecentActivities { get; set; } = []; public int WishlistCount { get; set; }
    public int OpenTicketsCount { get; set; }
    public int UnreadNotifications { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
}

public class CreateUserAddressDto
{
    public string Title { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class UpdateUserAddressDto
{
    public string Title { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class UpdateProfileDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}

public class ChangeUserStatusDto
{
    public bool IsActive { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmNewPassword { get; set; }
}