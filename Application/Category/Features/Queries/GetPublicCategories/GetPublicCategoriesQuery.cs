using Application.Cache.Contracts;
using Application.Category.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Category.Features.Queries.GetPublicCategories;

[RequestOutputCache(
    tags: [CacheTags.Category, CacheTags.Media],
    expirationInSeconds: 600)]
public record GetPublicCategoriesQuery(
    string? Search,
    int Page,
    int PageSize) : IPageQuery<CategoryDto>;
