using Application.Brand.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Brand.Features.Queries.GetBrands;

[RequestOutputCache(
    tags:
    [
        CacheTags.Brand,
        CacheTags.Category,
        CacheTags.Product,
        CacheTags.Media
    ],
    expirationInSeconds: 60)]
public record GetBrandsQuery(
    Guid? CategoryId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IPageQuery<BrandListItemDto>;