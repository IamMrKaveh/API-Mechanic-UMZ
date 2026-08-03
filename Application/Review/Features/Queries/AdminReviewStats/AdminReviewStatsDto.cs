namespace Application.Review.Features.Queries.AdminReviewStats;

public sealed record AdminReviewStatsDto(
    int Pending,
    int Approved,
    int Rejected,
    int Total);
