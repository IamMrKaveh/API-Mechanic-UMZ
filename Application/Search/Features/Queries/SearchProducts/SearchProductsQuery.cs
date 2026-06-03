using Application.Search.Features.Shared;

namespace Application.Search.Features.Queries.SearchProducts;

public sealed record SearchProductsQuery(
    string? Q,
    Guid? CategoryId,
    Guid? BrandId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool InStockOnly,
    string? SortBy,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<SearchResultDto<ProductSearchResultItemDto>>>;