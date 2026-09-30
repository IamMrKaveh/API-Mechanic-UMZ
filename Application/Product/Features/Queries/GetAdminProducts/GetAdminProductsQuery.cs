using Application.Product.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Product.Features.Queries.GetAdminProducts;

[RequestOutputCache(
    tags:
    [
        CacheTags.Product,
        CacheTags.ProductVariant,
        CacheTags.Category,
        CacheTags.Brand,
        CacheTags.Media,
        CacheTags.Inventory
    ],
    expirationInSeconds: 60)]
public record GetAdminProductsQuery(
    Guid? CategoryId,
    Guid? BrandId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page = 1,
    int PageSize = 20)
    : IPageQuery<ProductListItemDto>;