namespace DataAccessLayer.Models.DTO;

public enum ProductSortOptions
{
    Newest,
    Oldest,
    PriceAsc,
    PriceDesc,
    NameAsc,
    NameDesc,
    DiscountDesc,
    DiscountAsc
}

public class ProductSearchDto
{
    public string? Name { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public bool? HasDiscount { get; set; }
    public bool? IsUnlimited { get; set; }
    public ProductSortOptions SortBy { get; set; } = ProductSortOptions.Newest;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ProductStockDto
{
    [Range(1, 100000, ErrorMessage = "تعداد باید بین 1 تا 100000 باشد")]
    public int Quantity { get; set; }
    public int? VariantId { get; set; }
}

public class SetDiscountDto
{
    [Range(1, double.MaxValue)]
    public decimal OriginalPrice { get; set; }

    [Range(1, double.MaxValue)]
    public decimal DiscountedPrice { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام محصول الزامی است")]
    [StringLength(200, ErrorMessage = "نام محصول نمی‌تواند بیشتر از 200 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;

    public string? Sku { get; set; }
    public string? Description { get; set; }
    public List<IFormFile>? Files { get; set; }
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "گروه دسته‌بندی الزامی است")]
    public int CategoryGroupId { get; set; }

    public string? VariantsJson { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class CreateProductVariantDto
{
    public string? Sku { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Stock { get; set; }
    public bool IsUnlimited { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> AttributeValueIds { get; set; } = new();
}

public class AttributeValueDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string TypeDisplay { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public string? HexCode { get; set; }
}

public class MediaDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class ProductVariantResponseDto
{
    public int Id { get; set; }
    public string? Sku { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Stock { get; set; }
    public bool IsUnlimited { get; set; }
    public bool IsInStock { get; set; }
    public double DiscountPercentage { get; set; }
    public List<MediaDto> Images { get; set; } = new();
    public Dictionary<string, AttributeValueDto> Attributes { get; set; } = new();
}

public class PublicProductViewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public bool IsActive { get; set; }
    public int CategoryGroupId { get; set; }
    public object? CategoryGroup { get; set; }
    public List<ProductVariantResponseDto> Variants { get; set; } = new();
    public List<MediaDto> Images { get; set; } = new();
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int TotalStock { get; set; }
    public bool HasMultipleVariants { get; set; }
}

public class AdminProductViewDto : PublicProductViewDto
{
    public byte[]? RowVersion { get; set; }
}

public class ColorOptionDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام رنگ الزامی است")]
    [StringLength(50, ErrorMessage = "نام رنگ نمی‌تواند بیشتر از 50 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "کد رنگ نمی‌تواند بیشتر از 10 کاراکتر باشد")]
    public string? HexCode { get; set; } = "#FFFFFF";
}

public class SizeOptionDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام سایز الزامی است")]
    [StringLength(20, ErrorMessage = "نام سایز نمی‌تواند بیشتر از 20 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;
}

public class CategoryDto
{
    [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
    [StringLength(200, ErrorMessage = "نام دسته‌بندی نمی‌تواند بیشتر از 200 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;

    public IFormFile? IconFile { get; set; }
}

public class CategoryGroupDto
{
    [Required(ErrorMessage = "نام گروه دسته‌بندی الزامی است")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "شناسه دسته‌بندی الزامی است")]
    public int CategoryId { get; set; }

    public IFormFile? IconFile { get; set; }
}

public class ProductReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsVerifiedPurchase { get; set; }
}

public class CreateReviewDto
{
    [Required]
    public int ProductId { get; set; }
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }
    [StringLength(100)]
    public string? Title { get; set; }
    [StringLength(2000)]
    public string? Comment { get; set; }
}