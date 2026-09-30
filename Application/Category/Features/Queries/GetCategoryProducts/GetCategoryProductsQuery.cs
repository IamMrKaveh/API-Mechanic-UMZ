using Application.Category.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Category.Features.Queries.GetCategoryProducts;

[RequestOutputCache(
    tags:
    [
        CacheTags.Category,
        CacheTags.Product,
        CacheTags.ProductVariant,
        CacheTags.Brand,
        CacheTags.Media,
        CacheTags.Inventory
    ],
    expirationInSeconds: 60)]
public record GetCategoryProductsQuery(
    Guid CategoryId,
    bool ActiveOnly,
    int Page,
    int PageSize) : IPageQuery<CategoryProductItemDto>;