namespace Application.Search.Features.Queries.GetSearchIndexStats;

public sealed record GetSearchIndexStatsQuery : IRequest<ServiceResult<SearchIndexStatsDto>>;

public sealed record SearchIndexStatsDto(
    long ProductsCount,
    long CategoriesCount,
    long BrandsCount,
    long TotalDocuments);