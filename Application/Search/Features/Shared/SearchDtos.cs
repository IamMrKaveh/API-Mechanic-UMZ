namespace Application.Search.Features.Shared;

public record SearchProductsParams
{
    public string Q { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public string? Brand { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool InStockOnly { get; init; }
    public string? BrandName { get; init; }
    public List<string>? Tags { get; init; }
    public string? SortBy { get; init; }
}

public record SearchResultDto<T>
{
    public List<T> Items { get; init; } = [];
    public long Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public record ProductSearchResultItemDto
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Slug { get; init; }
    public string? Sku { get; init; }
    public string? CategoryName { get; init; }
    public Guid CategoryId { get; init; }
    public string? BrandName { get; init; }
    public Guid BrandId { get; init; }
    public decimal Price { get; init; }
    public decimal? DiscountedPrice { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public string? ImageUrl { get; init; }
    public List<string> Images { get; init; } = [];
    public bool IsActive { get; init; }
    public bool InStock { get; init; }
    public int StockQuantity { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int SalesCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record GlobalSearchResultDto
{
    public List<ProductSearchResultItemDto> Products { get; init; } = [];
    public List<CategorySearchSummaryDto> Categories { get; init; } = [];
    public List<BrandSearchSummaryDto> Brands { get; init; } = [];
    public string Query { get; init; } = string.Empty;
}

public record CategorySearchSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
}

public record BrandSearchSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
}

public record ProductSearchDocument
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public Guid BrandId { get; init; }
    public string BrandSlug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public double? DiscountedPrice { get; init; }
    public double? DiscountPercentage { get; init; }
    public List<string> Images { get; init; } = [];
    public string ImageUrl { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public bool IsActive { get; init; }
    public bool InStock { get; init; }
    public int StockQuantity { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int SalesCount { get; init; }
    public List<string> Tags { get; init; } = [];
}

public record CategorySearchDocument
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
    public string? Icon { get; init; }
}

public record BrandSearchDocument
{
    public Guid BrandId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
    public string? Icon { get; init; }
}

public record FailedElasticOperation
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? LastRetryAt { get; set; }
}