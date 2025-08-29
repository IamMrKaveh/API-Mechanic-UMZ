namespace DataAccessLayer.Models.DTO;

public class ProductDto
{
    public int Id { get; set; }
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    public string? Icon { get; set; }
    [Range(0, int.MaxValue)]
    public int? PurchasePrice { get; set; }
    [Range(0, int.MaxValue)]
    public int? SellingPrice { get; set; }
    [Range(0, int.MaxValue)]
    public int? Count { get; set; }
    public int? ProductTypeId { get; set; }
}

public class ProductTypeDto
{
    public int Id { get; set; }
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    public string? Icon { get; set; }
}

public class ProductSearchDto
{
    public string? Name { get; set; }
    public int? ProductTypeId { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ProductStockDto
{
    [Required]
    public int ProductId { get; set; }
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public class SetDiscountDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Original price must be greater than 0")]
    public int OriginalPrice { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Discounted price must be greater than 0")]
    public int DiscountedPrice { get; set; }
}
