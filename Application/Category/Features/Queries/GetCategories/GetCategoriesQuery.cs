using Application.Category.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Category.Features.Queries.GetCategories;

[RequestOutputCache(
    tags:
    [
        CacheTags.Category,
        CacheTags.Media,
        CacheTags.Product,
        CacheTags.Brand
    ],
    expirationInSeconds: 60)]
public record GetCategoriesQuery(
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IPageQuery<CategoryListItemDto>;