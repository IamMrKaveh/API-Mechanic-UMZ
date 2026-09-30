using Application.Analytics.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Analytics.Features.Queries.GetRevenueReport;

[RequestOutputCache(
    tags:
    [
        CacheTags.Manual.Analytics
    ],
    expirationInSeconds: 600)]
public sealed record GetRevenueReportQuery(
    DateTime FromDate,
    DateTime ToDate) : IQuery<RevenueReportDto>;
