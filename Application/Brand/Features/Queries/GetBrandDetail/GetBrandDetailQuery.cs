using Application.Brand.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Brand.Features.Queries.GetBrandDetail;

[RequestOutputCache(
    tags:
    [
        CacheTags.Brand,
        CacheTags.Category,
        CacheTags.Product,
        CacheTags.Media
    ],
    expirationInSeconds: 120)]
public record GetBrandDetailQuery(Guid BrandId) : IQuery<BrandDetailDto?>;