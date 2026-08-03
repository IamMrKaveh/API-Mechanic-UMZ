namespace Application.Review.Features.Shared;

public sealed record AdminReviewFilter(
    string Status,
    int Page,
    int PageSize,
    string? SearchText,
    int? MinRating,
    Guid? ProductId,
    DateTime? DateFrom,
    DateTime? DateTo);
