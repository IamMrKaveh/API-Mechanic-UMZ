namespace Application.Search.Features.Queries.SearchProducts;

public record SearchProductsQuery : IRequest<ServiceResult<SearchResultDto<ProductSearchResultItemDto>>>
{
    public string? Q { get; init; }
    public int? CategoryId { get; init; }
    public int? BrandId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Brand { get; init; }
    public bool InStockOnly { get; init; }
    public string? SortBy { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public List<string>? Tags { get; init; }
}