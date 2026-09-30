using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Review.Features.Queries.AdminReviewStats;

[RequestOutputCache(
    tags:
    [
        CacheTags.ProductReview
    ],
    expirationInSeconds: 60)]
public sealed record GetAdminReviewStatsQuery : IQuery<AdminReviewStatsDto>;
