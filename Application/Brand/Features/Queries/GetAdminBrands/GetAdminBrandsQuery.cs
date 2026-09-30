using Application.Brand.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Brand.Features.Queries.GetAdminBrands;

[RequestOutputCache(
    tags:
    [
        CacheTags.Brand,
        CacheTags.Category,
        CacheTags.Product,
        CacheTags.Media
    ],
    expirationInSeconds: 60)]
public record GetAdminBrandsQuery(
    Guid? CategoryId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IPageQuery<BrandListItemDto>;