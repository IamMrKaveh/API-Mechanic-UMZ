using Application.Analytics.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Analytics.Features.Queries.GetSalesChartData;

[RequestOutputCache(
    tags:
    [
        CacheTags.Manual.Analytics
    ],
    expirationInSeconds: 900)]
public sealed record GetSalesChartDataQuery(
    DateTime FromDate,
    DateTime ToDate,
    string GroupBy = "day",
    int Page = 1,
    int PageSize = 10) : IPageQuery<SalesChartDataPointDto>;
