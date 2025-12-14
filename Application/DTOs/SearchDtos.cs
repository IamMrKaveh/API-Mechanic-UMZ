namespace Application.DTOs;

public class ProductSearchDocument
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string CategoryName { get; init; } = default!;
    public int CategoryId { get; init; }
    public string CategoryGroupName { get; init; } = default!;
    public int CategoryGroupId { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public bool HasDiscount { get; init; }
    public bool IsInStock { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ImageUrl { get; init; }
}

public class CategorySearchDocument
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public string? IconUrl { get; init; }
}

public class CategoryGroupSearchDocument
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public string CategoryName { get; init; } = default!;
    public int CategoryId { get; init; }
    public string? IconUrl { get; init; }
}

public sealed record SearchProductsQuery(
    string? Q,
    int? CategoryId,
    int? CategoryGroupId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? IsInStock,
    bool? HasDiscount,
    int Page = 1,
    int PageSize = 20,
    string? Sort = null
);

public sealed record GlobalSearchQuery(
    string Q
);

public class SearchResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IDictionary<int, IDictionary<string, string[]>> Highlights { get; set; } = new Dictionary<int, IDictionary<string, string[]>>();
}

public class GlobalSearchResultDto
{
    public IEnumerable<CategorySearchDocument> Categories { get; set; } = [];
    public IEnumerable<CategoryGroupSearchDocument> CategoryGroups { get; set; } = [];
    public IEnumerable<ProductSearchDocument> Products { get; set; } = [];
}
