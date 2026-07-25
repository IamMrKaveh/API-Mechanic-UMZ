namespace Application.User.Features.Shared;

public record AdminUserListItemDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsAdmin { get; init; }
    public bool IsEmailVerified { get; init; }
    public bool IsLockedOut { get; init; }
    public bool IsDeleted { get; init; }
    public List<string> Roles { get; init; } = [];
    public int OrderCount { get; init; }
    public int CompletedOrderCount { get; init; }
    public decimal TotalSpent { get; init; }
    public string? DefaultAddressSummary { get; init; }
    public int AddressCount { get; init; }
    public decimal WalletBalance { get; init; }
    public int OpenTicketsCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record AdminUserFilterParams(
    string? Search,
    string? Role,
    bool? IsActive,
    bool? IsAdmin,
    decimal? MinTotalSpent,
    DateTime? CreatedAfter,
    bool IncludeDeleted,
    int Page,
    int PageSize);
