namespace Application.Shipping.Features.Shared;

public record ShippingDto
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

public record ShippingCreateDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal BaseCost { get; init; }
    public string? EstimatedDeliveryTime { get; init; }
    public int MinDeliveryDays { get; init; } = 1;
    public int MaxDeliveryDays { get; init; } = 7;
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
}

public record ShippingUpdateDto
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

public record AvailableShippingDto
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

public record ShippingCostResultDto
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
    public List<ShippingCostItemDetailDto> ItemDetails { get; init; } = new();
}

public record ShippingCostItemDetailDto
{
    public int VariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal ShippingMultiplier { get; init; }
}

public record ProductVariantShippingDto
{
    public int Id { get; init; }
    public int ProductVariantId { get; init; }
    public int ShippingId { get; init; }
    public string ShippingName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public record UpdateProductVariantShippingsDto
{
    public int ProductVariantId { get; init; }
    public decimal ShippingMultiplier { get; init; } = 1;
    public List<int> EnabledShippingIds { get; init; } = new();
}

public record ProductVariantShippingInfoDto
{
    public int VariantId { get; init; }
    public string? ProductName { get; init; }
    public string? VariantDisplayName { get; init; }
    public decimal ShippingMultiplier { get; init; }
    public List<ShippingSelectionDto> AvailableShippings { get; init; } = new();
}

public record ShippingSelectionDto
{
    public int ShippingId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal BaseCost { get; init; }
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
}