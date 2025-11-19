namespace Application.DTOs;

public class CreateOrderItemDto
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public int VariantId { get; set; }

    [Required]
    [Range(1, 1000)]
    public int Quantity { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal SellingPrice { get; set; }
}

public class UpdateOrderItemDto
{
    [Range(0, 1000)]
    public int? Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SellingPrice { get; set; }

    [Required]
    public string RowVersion { get; set; }
}