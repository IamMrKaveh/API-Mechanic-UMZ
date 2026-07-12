using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetReviewById;

public sealed record GetReviewByIdQuery(
    Guid ReviewId)
    : IQuery<ProductReviewDto>;