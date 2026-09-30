using Application.Category.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Category.Features.Queries.GetAdminCategories;

[RequestOutputCache(
    tags:
    [
        CacheTags.Category,
        CacheTags.Media,
        CacheTags.Product,
        CacheTags.Brand
    ],
    expirationInSeconds: 60)]
public record GetAdminCategoriesQuery(
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IPageQuery<CategoryListItemDto>;