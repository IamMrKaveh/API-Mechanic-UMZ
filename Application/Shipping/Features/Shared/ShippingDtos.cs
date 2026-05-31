namespace Application.Shipping.Features.Shared;

public record ShippingDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal BaseCost { get; init; }
    public string? EstimatedDeliveryTime { get; init; }
    public int MinDeliveryDays { get; init; }
    public int MaxDeliveryDays { get; init; }
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public int SortOrder { get; init; }
    public decimal? FreeShippingThreshold { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public decimal? MaxOrderAmount { get; init; }
    public decimal? MaxWeight { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record ShippingListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal BaseCost { get; init; }
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public int SortOrder { get; init; }
    public string DeliveryTimeDisplay { get; init; } = string.Empty;
}

public record ShippingCostResultDto
{
    public Guid ShippingId { get; init; }
    public string ShippingName { get; init; } = string.Empty;
    public decimal Cost { get; init; }
    public bool IsFree { get; init; }
    public int MinDeliveryDays { get; init; }
    public int MaxDeliveryDays { get; init; }
}

public record AvailableShippingDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Cost { get; init; }
    public bool IsFree { get; init; }
    public string DeliveryTimeDisplay { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

public record ProductVariantShippingInfoDto
{
    public Guid VariantId { get; init; }
    public decimal ShippingMultiplier { get; init; }
    public List<ShippingListItemDto> AvailableShippings { get; init; } = [];
    public List<int> EnabledShippingIds { get; init; } = [];
}