namespace Application.Search.Features.Shared;

// ========== Search Parameters ==========

public class SearchProductsParams
{
    public string Q { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }
    public string? Brand { get; set; }
    public List<string>? Tags { get; set; }
    public string? SortBy { get; set; }
}

// ========== Search Result DTOs ==========

public class SearchResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// آیتم نتیجه جستجوی محصول - DTO خالص بدون وابستگی به Elasticsearch
/// </summary>
public class ProductSearchResultItemDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public string? Sku { get; set; }
    public string? CategoryName { get; set; }
    public int CategoryId { get; set; }
    public string? BrandName { get; set; }
    public int BrandId { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Images { get; set; } = new();
    public bool IsActive { get; set; }
    public bool InStock { get; set; }
    public int StockQuantity { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int SalesCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GlobalSearchResultDto
{
    public List<ProductSearchResultItemDto> Products { get; set; } = new();
    public List<CategorySearchSummaryDto> Categories { get; set; } = new();
    public List<BrandSearchSummaryDto> Brands { get; set; } = new();
    public string Query { get; set; } = string.Empty;
}

public class CategorySearchSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class BrandSearchSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

// ========== Elasticsearch Documents (used by Infrastructure) ==========

public class ProductSearchDocument
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public decimal Price { get; set; }
    public double? DiscountedPrice { get; set; }
    public double? DiscountPercentage { get; set; }
    public List<string> Images { get; set; } = new();
    public string ImageUrl { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public bool InStock { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int SalesCount { get; set; }
    public List<string> Tags { get; set; } = new();
    public Domain.Brand.Brand Brand { get; set; }
}

public class CategorySearchDocument
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public string? Icon { get; set; }
}

public class BrandSearchDocument
{
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public string? Icon { get; set; }
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

public class ElasticsearchOutboxMessage
{
    public int Id { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OperationType { get; set; }
    public string? Payload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Document { get; set; }
    public string? ChangeType { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

public class FailedIndexOperation
{
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string Document { get; set; } = default!;
    public string Error { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public int RetryCount { get; set; }
}

public enum EntityChangeType
{
    Created,
    Updated,
    Deleted
}