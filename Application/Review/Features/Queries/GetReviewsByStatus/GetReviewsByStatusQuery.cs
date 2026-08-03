using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetReviewsByStatus;

public sealed record GetReviewsByStatusQuery(
    string Status,
    int Page = 1,
    int PageSize = 10,
    string? SearchText = null,
    int? MinRating = null,
    Guid? ProductId = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null)
    : IPageQuery<ProductReviewDto>;
