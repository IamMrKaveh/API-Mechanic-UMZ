namespace Application.Search.Features.Shared;

// ========== Search Parameters ==========

public class SearchProductsParams
{
    public string Q { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int? CategoryId { get; init; }
    public int? BrandId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool InStockOnly { get; init; }
    public string? Brand { get; init; }
    public List<string>? Tags { get; init; }
    public string? SortBy { get; init; }
}

// ========== Search Result DTOs ==========

public class SearchResultDto<T>
{
    public List<T> Items { get; init; } = new();
    public long Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// آیتم نتیجه جستجوی محصول - DTO خالص بدون وابستگی به Elasticsearch
/// </summary>
public class ProductSearchResultItemDto
{
    public int ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Slug { get; init; }
    public string? Sku { get; init; }
    public string? CategoryName { get; init; }
    public int CategoryId { get; init; }
    public string? BrandName { get; init; }
    public int BrandId { get; init; }
    public decimal Price { get; init; }
    public decimal? DiscountedPrice { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public string? ImageUrl { get; init; }
    public List<string> Images { get; init; } = new();
    public bool IsActive { get; init; }
    public bool InStock { get; init; }
    public int StockQuantity { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int SalesCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GlobalSearchResultDto
{
    public List<ProductSearchResultItemDto> Products { get; init; } = new();
    public List<CategorySearchSummaryDto> Categories { get; init; } = new();
    public List<BrandSearchSummaryDto> Brands { get; init; } = new();
    public string Query { get; init; } = string.Empty;
}

public class CategorySearchSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
}

public class BrandSearchSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
}

// ========== Elasticsearch Documents (used by Infrastructure) ==========

public class ProductSearchDocument
{
    public int ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public int BrandId { get; init; }
    public decimal Price { get; init; }
    public double? DiscountedPrice { get; init; }
    public double? DiscountPercentage { get; init; }
    public List<string> Images { get; init; } = new();
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
    public List<string> Tags { get; init; } = new();
    public Domain.Brand.Brand Brand { get; init; }
}

public class CategorySearchDocument
{
    public int CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
    public string? Icon { get; init; }
}

public class BrandSearchDocument
{
    public int BrandId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
    public string? Icon { get; init; }
}

// ========== Outbox & DLQ Entities ==========

public class FailedElasticOperation
{
    public int Id { get; set; }
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

public enum EntityChangeType
{
    Created,
    Updated,
    Deleted
}