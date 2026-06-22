namespace Application.Search.Features.Queries.GetSearchIndexStats;

public sealed record GetSearchIndexStatsQuery
    : IQuery<SearchIndexStatsDto>;

public sealed record SearchIndexStatsDto(
    long ProductsCount,
    long CategoriesCount,
    long BrandsCount,
    long TotalDocuments);