using Application.Analytics.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Analytics.Features.Queries.GetTopSellingProducts;

[RequestOutputCache(
    tags:
    [
        CacheTags.Manual.Analytics
    ],
    expirationInSeconds: 900)]
public sealed record GetTopSellingProductsQuery(
    int Count = 10,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 10) : IPageQuery<TopSellingProductDto>;
