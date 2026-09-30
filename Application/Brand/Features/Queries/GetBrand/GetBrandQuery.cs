using Application.Brand.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Brand.Features.Queries.GetBrand;

[RequestOutputCache(
    tags:
    [
        CacheTags.Brand,
        CacheTags.Category,
        CacheTags.Product,
        CacheTags.Media
    ],
    expirationInSeconds: 60)]
public record GetBrandQuery(Guid Id) : IQuery<BrandDetailDto>;