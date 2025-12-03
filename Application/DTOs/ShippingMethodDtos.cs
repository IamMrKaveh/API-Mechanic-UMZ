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