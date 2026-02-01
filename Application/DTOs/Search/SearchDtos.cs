namespace Application.DTOs.Search;

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
}

public class GlobalSearchResultDto
{
    // Using generic summary DTOs instead of Infrastructure Documents
    public List<ProductSummaryDto> Products { get; set; } = new();
    public List<CategorySearchSummaryDto> Categories { get; set; } = new();
    public List<CategoryGroupSearchSummaryDto> CategoryGroups { get; set; } = new();
    public string Query { get; set; } = string.Empty;
}

public class CategorySearchSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class CategoryGroupSearchSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}