using Application.Search.Features.Shared;

namespace Application.Search.Features.Queries.SearchProducts;

public sealed record SearchProductsQuery(
    string? Q,
    Guid? CategoryId,
    Guid? BrandId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Brand,
    bool InStockOnly,
    string? SortBy,
    List<string>? Tags) : IRequest<ServiceResult<SearchResultDto<ProductSearchResultItemDto>>>