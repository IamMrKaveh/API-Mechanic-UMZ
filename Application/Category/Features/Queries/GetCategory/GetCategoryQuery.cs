using Application.Category.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Category.Features.Queries.GetCategory;

[RequestOutputCache(
    tags:
    [
        CacheTags.Category,
        CacheTags.Media,
        CacheTags.Product,
        CacheTags.Brand
    ],
    expirationInSeconds: 60)]
public record GetCategoryQuery(Guid Id) : IQuery<CategoryDetailDto>;