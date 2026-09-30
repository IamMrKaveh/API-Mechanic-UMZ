using Application.Analytics.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Analytics.Features.Queries.GetDashboardStatistics;

[RequestOutputCache(
    tags:
    [
        CacheTags.Manual.Analytics
    ],
    expirationInSeconds: 600)]
public sealed record GetDashboardStatisticsQuery(
    DateTime? FromDate,
    DateTime? ToDate) : IQuery<DashboardStatisticsDto>;
