namespace Application.Shipping.Features.Shared;

public class ShippingMethodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Cost { get; set; }
    public string? EstimatedDeliveryTime { get; set; }
    public int MinDeliveryDays { get; set; }
    public int MaxDeliveryDays { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxOrderAmount { get; set; }
    public bool IsFreeAboveAmount { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public string? RowVersion { get; set; }
}

public class ShippingMethodCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BaseCost { get; set; }
    public string? EstimatedDeliveryTime { get; set; }
    public int MinDeliveryDays { get; set; } = 1;
    public int MaxDeliveryDays { get; set; } = 7;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class ShippingMethodUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BaseCost { get; set; }
    public string? EstimatedDeliveryTime { get; set; }
    public int MinDeliveryDays { get; set; }
    public int MaxDeliveryDays { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string? RowVersion { get; set; }
}

public class AvailableShippingMethodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BaseCost { get; set; }
    public decimal TotalMultiplier { get; set; }
    public decimal FinalCost { get; set; }
    public bool IsFreeShipping { get; set; }
    public string? Description { get; set; }
    public string? EstimatedDeliveryTime { get; set; }
    public int MinDeliveryDays { get; set; }
    public int MaxDeliveryDays { get; set; }
}

public class ShippingCostResultDto
{
    public int ShippingMethodId { get; set; }
    public string ShippingMethodName { get; set; } = string.Empty;
    public decimal BaseCost { get; set; }
    public decimal TotalMultiplier { get; set; }
    public decimal FinalCost { get; set; }
    public bool IsFreeShipping { get; set; }
    public decimal OrderSubtotal { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public decimal? RemainingForFreeShipping { get; set; }
    public string? EstimatedDeliveryTime { get; set; }
    public List<ShippingCostItemDetailDto> ItemDetails { get; set; } = new();
}

public class ShippingCostItemDetailDto
{
    public int VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal ShippingMultiplier { get; set; }
}

public class ProductVariantShippingMethodDto
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public int ShippingMethodId { get; set; }
    public string ShippingMethodName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class UpdateProductVariantShippingMethodsDto
{
    public int ProductVariantId { get; set; }
    public decimal ShippingMultiplier { get; set; } = 1;
    public List<int> EnabledShippingMethodIds { get; set; } = new();
}

public class ProductVariantShippingInfoDto
{
    public int VariantId { get; set; }
    public string? ProductName { get; set; }
    public string? VariantDisplayName { get; set; }
    public decimal ShippingMultiplier { get; set; }
    public List<ShippingMethodSelectionDto> AvailableShippingMethods { get; set; } = new();
}

public class ShippingMethodSelectionDto
{
    public int ShippingMethodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BaseCost { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
}