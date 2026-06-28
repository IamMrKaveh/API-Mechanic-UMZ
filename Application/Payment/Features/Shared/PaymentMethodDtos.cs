namespace Application.Payment.Features.Shared;

public record PaymentMethodDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public decimal FeeAmount { get; init; }
    public decimal FeePercentage { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record PaymentMethodListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public decimal FeeAmount { get; init; }
    public decimal FeePercentage { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public int SortOrder { get; init; }
}

public record AvailablePaymentMethodDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public string? Description { get; init; }
    public decimal Fee { get; init; }
    public int SortOrder { get; init; }
}