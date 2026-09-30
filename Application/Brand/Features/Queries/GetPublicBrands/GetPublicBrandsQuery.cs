using Application.Brand.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Brand.Features.Queries.GetPublicBrands;

[RequestOutputCache(
    tags:
    [
        CacheTags.Brand,
        CacheTags.Category,
        CacheTags.Product,
        CacheTags.Media
    ],
    expirationInSeconds: 1800)]
public sealed record GetPublicBrandsQuery(Guid? CategoryId) : IQuery<IReadOnlyList<BrandListItemDto>>;
