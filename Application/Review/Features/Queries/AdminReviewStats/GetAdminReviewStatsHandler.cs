namespace Application.Review.Features.Queries.AdminReviewStats;

public sealed class GetAdminReviewStatsHandler(
    IReviewQueryService reviewQueryService,
    ILogger<GetAdminReviewStatsHandler> logger)
    : IQueryHandler<GetAdminReviewStatsQuery, AdminReviewStatsDto>
{
    public async Task<ServiceResult<AdminReviewStatsDto>> Handle(
        GetAdminReviewStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var stats = await reviewQueryService.GetAdminReviewStatsAsync(cancellationToken);
            return ServiceResult<AdminReviewStatsDto>.Success(stats);
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database error while loading admin review stats.");
            return ServiceResult<AdminReviewStatsDto>.Failure(
                Error.Infrastructure("خطا در دریافت آمار نظرات."));
        }
    }
}
