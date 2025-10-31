namespace DataAccessLayer.Models.DTO;

public class ProductDto
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public IFormFile? IconFile { get; set; }

    public string[] Colors { get; set; } = Array.Empty<string>();

    public string[] Sizes { get; set; } = Array.Empty<string>();

    [Range(1, int.MaxValue)]
    public decimal PurchasePrice { get; set; }

    [Range(0, int.MaxValue)]
    public decimal OriginalPrice { get; set; }

    [Range(1, int.MaxValue)]
    public decimal SellingPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int Count { get; set; }

    public bool IsUnlimited { get; set; }

    public int CategoryId { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class PublicProductViewDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string[] Colors { get; set; } = Array.Empty<string>();

    public string[] Sizes { get; set; } = Array.Empty<string>();

    public decimal OriginalPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public bool HasDiscount => OriginalPrice > SellingPrice;

    public double DiscountPercentage
    {
        get
        {
            if (HasDiscount && OriginalPrice > 0)
            {
                return Math.Max(0, (double)(OriginalPrice - SellingPrice) * 100 / Convert.ToDouble(OriginalPrice));
            }
            return 0;
        }
    }

    public int Count { get; set; }

    public bool IsUnlimited { get; set; }

    public int CategoryId { get; set; }

    public object? Category { get; set; }
}

public class AdminProductViewDto : PublicProductViewDto
{
    public decimal? PurchasePrice { get; set; }
}

public class CategoryDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public IFormFile? IconFile { get; set; }

    public int Id { get; set; }

    public byte[]? RowVersion { get; set; }
}

public enum ProductSortOptions
{
    Newest,
    Oldest,
    PriceAsc,
    PriceDesc,
    NameAsc,
    NameDesc,
    DiscountAsc,
    DiscountDesc
}

public class ProductSearchDto
{
    public string? Name { get; set; }

    public int? CategoryId { get; set; }

    public int? MinPrice { get; set; }

    public int? MaxPrice { get; set; }

    public bool? InStock { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public ProductSortOptions SortBy { get; set; } = ProductSortOptions.Newest;
}

public class ProductStockDto
{
    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public string? Notes { get; set; }
}

public class SetDiscountDto
{
    [Required, Range(1, int.MaxValue)]
    public int OriginalPrice { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int DiscountedPrice { get; set; }
}