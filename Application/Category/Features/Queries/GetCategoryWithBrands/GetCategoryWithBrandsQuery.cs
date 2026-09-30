using Application.Category.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Category.Features.Queries.GetCategoryWithBrands;

[RequestOutputCache(
    tags:
    [
        CacheTags.Category,
        CacheTags.Brand,
        CacheTags.Media
    ],
    expirationInSeconds: 120)]
public record GetCategoryWithBrandsQuery(Guid CategoryId)
    : IQuery<CategoryWithBrandsDto?>;