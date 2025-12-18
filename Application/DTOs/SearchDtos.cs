namespace Application.DTOs;

/// <summary>
/// Document Models
/// </summary>
public class ProductSearchDocument
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryGroupName { get; set; } = string.Empty;
    public int CategoryGroupId { get; set; }
    public float Price { get; set; }
    public float? DiscountedPrice { get; set; }
    public float? DiscountPercentage { get; set; }
    public List<string> Images { get; set; } = new();
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool InStock { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public float? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int SalesCount { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Brand { get; set; } = string.Empty;
}

public class CategorySearchDocument
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class CategoryGroupSearchDocument
{
    public int CategoryGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

/// <summary>
/// DTOs
/// </summary>
public class SearchProductsQuery
{
    public string Q { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? CategoryId { get; set; }
    public int? CategoryGroupId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }
    public string? Brand { get; set; }
    public List<string>? Tags { get; set; }
    public string? SortBy { get; set; }
}

public class SearchResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public Dictionary<string, List<string>> Highlights { get; set; } = new();
    public Dictionary<string, object> Aggregations { get; set; } = new();
    public int Took { get; set; }
    public double? MaxScore { get; set; }
}

public class GlobalSearchResultDto
{
    public List<ProductSearchDocument> Products { get; set; } = new();
    public List<CategorySearchDocument> Categories { get; set; } = new();
    public List<CategoryGroupSearchDocument> CategoryGroups { get; set; } = new();
    public string Query { get; set; } = string.Empty;
}