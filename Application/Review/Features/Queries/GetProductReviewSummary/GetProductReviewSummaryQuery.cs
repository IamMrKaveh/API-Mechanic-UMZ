using Application.Review.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Review.Features.Queries.GetProductReviewSummary;

[RequestOutputCache(
    tags:
    [
        CacheTags.ProductReview
    ],
    expirationInSeconds: 120)]
public sealed record GetProductReviewSummaryQuery(
    Guid ProductId)
    : IQuery<ReviewSummaryDto>;