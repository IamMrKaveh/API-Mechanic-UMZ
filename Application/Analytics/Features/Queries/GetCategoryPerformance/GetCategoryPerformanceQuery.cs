using Application.Analytics.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Analytics.Features.Queries.GetCategoryPerformance;

[RequestOutputCache(
    tags:
    [
        CacheTags.Manual.Analytics
    ],
    expirationInSeconds: 900)]
public sealed record GetCategoryPerformanceQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 10) : IPageQuery<CategoryPerformanceDto>;
