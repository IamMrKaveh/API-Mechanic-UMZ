using Application.Analytics.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Analytics.Features.Queries.GetInventoryReport;

[RequestOutputCache(
    tags:
    [
        CacheTags.Manual.Analytics
    ],
    expirationInSeconds: 300)]
public sealed record GetInventoryReportQuery : IQuery<InventoryReportDto>;
