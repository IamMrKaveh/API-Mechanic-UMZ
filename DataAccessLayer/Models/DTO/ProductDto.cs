namespace DataAccessLayer.Models.DTO;

public class ProductDto
{
    private string _name = string.Empty;

    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get => _name; set => _name = new HtmlSanitizer().Sanitize(value); }

    public IFormFile? IconFile { get; set; }

    [Range(1, int.MaxValue)]
    public int PurchasePrice { get; set; }
    [Range(1, int.MaxValue)]
    public int SellingPrice { get; set; }
    [Range(0, int.MaxValue)]
    public int Count { get; set; }
    public bool IsUnlimited { get; set; }
    public int CategoryId { get; set; }
    [Range(0, int.MaxValue)]
    public int OriginalPrice { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class PublicProductViewDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public int OriginalPrice { get; set; }
    public int SellingPrice { get; set; }
    public bool HasDiscount { get; set; }
    public double DiscountPercentage { get; set; }
    public int Count { get; set; }
    public bool IsUnlimited { get; set; }
    public int CategoryId { get; set; }
    public object? Category { get; set; }
}

public class AdminProductViewDto : PublicProductViewDto
{
    public int? PurchasePrice { get; set; }
}

public class CategoryDto
{
    private string _name = string.Empty;
    [Required, StringLength(100)]
    public string Name { get => _name; set => _name = new HtmlSanitizer().Sanitize(value); }

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
    private string? _name;
    public string? Name { get => _name; set => _name = value == null ? null : new HtmlSanitizer().Sanitize(value); }
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
    private string? _notes;
    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    public string? Notes { get => _notes; set => _notes = value == null ? null : new HtmlSanitizer().Sanitize(value); }
}

public class SetDiscountDto
{
    [Required, Range(1, int.MaxValue)]
    public int OriginalPrice { get; set; }
    [Required, Range(1, int.MaxValue)]
    public int DiscountedPrice { get; set; }
}