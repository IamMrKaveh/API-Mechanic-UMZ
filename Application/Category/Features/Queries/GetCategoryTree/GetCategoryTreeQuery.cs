using Application.Category.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Category.Features.Queries.GetCategoryTree;

[RequestOutputCache(
    tags:
    [
        CacheTags.Category,
        CacheTags.Media,
        CacheTags.Product
    ],
    expirationInSeconds: 3600)]
public record GetCategoryTreeQuery : IQuery<IReadOnlyList<CategoryTreeDto>>;
