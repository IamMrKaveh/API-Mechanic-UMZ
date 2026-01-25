namespace Application.DTOs;

public class ShippingMethodDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Cost { get; set; }
    public string? Description { get; set; }
    public string? EstimatedDeliveryTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string? RowVersion { get; set; }
}

public class ShippingMethodCreateDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Range(0, (double)decimal.MaxValue)]
    public decimal Cost { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? EstimatedDeliveryTime { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ShippingMethodUpdateDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Range(0, (double)decimal.MaxValue)]
    public decimal Cost { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? EstimatedDeliveryTime { get; set; }

    public bool IsActive { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class AvailableShippingMethodDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal BaseCost { get; set; }

    public decimal TotalMultiplier { get; set; }

    public decimal FinalCost { get; set; }

    public string? Description { get; set; }

    public string? EstimatedDeliveryTime { get; set; }
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
    public string ProductName { get; set; } = string.Empty;
    public string VariantDisplayName { get; set; } = string.Empty;
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