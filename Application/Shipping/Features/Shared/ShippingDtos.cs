namespace Application.Shipping.Features.Shared;

public sealed record ShippingDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Cost { get; init; }
    public string? EstimatedDeliveryTime { get; init; }
    public int MinDeliveryDays { get; init; }
    public int MaxDeliveryDays { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public bool IsDefault { get; init; }
    public int SortOrder { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public decimal? MaxOrderAmount { get; init; }
    public bool IsFreeAboveAmount { get; init; }
    public decimal? FreeShippingThreshold { get; init; }
    public string? RowVersion { get; init; }
}

public sealed record ShippingCreateDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal BaseCost { get; init; }
    public string? EstimatedDeliveryTime { get; init; }
    public int MinDeliveryDays { get; init; }
    public int MaxDeliveryDays { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}

public sealed record ShippingUpdateDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal BaseCost { get; init; }
    public string? EstimatedDeliveryTime { get; init; }
    public int MinDeliveryDays { get; init; }
    public int MaxDeliveryDays { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public string? RowVersion { get; init; }
}

public sealed record AvailableShippingDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal BaseCost { get; init; }
    public decimal TotalMultiplier { get; init; }
    public decimal FinalCost { get; init; }
    public bool IsFreeShipping { get; init; }
    public string? Description { get; init; }
    public string? EstimatedDeliveryTime { get; init; }
    public int MinDeliveryDays { get; init; }
    public int MaxDeliveryDays { get; init; }
}

public sealed record ShippingCostResultDto
{
    public int ShippingId { get; init; }
    public string ShippingName { get; init; } = string.Empty;
    public decimal BaseCost { get; init; }
    public decimal TotalMultiplier { get; init; }
    public decimal FinalCost { get; init; }
    public bool IsFreeShipping { get; init; }
    public decimal OrderSubtotal { get; init; }
    public decimal? FreeShippingThreshold { get; init; }
    public decimal? RemainingForFreeShipping { get; init; }
    public string? EstimatedDeliveryTime { get; init; }
    public List<ShippingCostItemDetailDto> ItemDetails { get; init; } = [];
}

public sealed record ShippingCostItemDetailDto
{
    public int VariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal ShippingMultiplier { get; init; }
}

public sealed record ProductVariantShippingDto(
    int Id,
    int ProductVariantId,
    int ShippingId,
    string ShippingName,
    bool IsActive
);

public sealed record UpdateProductVariantShippingsDto
{
    public int ProductVariantId { get; init; }
    public decimal ShippingMultiplier { get; init; }
    public List<int> EnabledShippingIds { get; init; } = [];
}

public sealed record ProductVariantShippingInfoDto
{
    public int VariantId { get; init; }
    public string? ProductName { get; init; }
    public string? VariantDisplayName { get; init; }
    public decimal ShippingMultiplier { get; init; }
    public List<ShippingSelectionDto> AvailableShippings { get; init; } = [];
}

public sealed record ShippingSelectionDto
{
    public int ShippingId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal BaseCost { get; init; }
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
}