using System.Data.Common;
using Application.Review.Features.Shared;
using Microsoft.Extensions.Logging;

namespace Application.Review.Features.Queries.GetReviewsByStatus;

public sealed class GetReviewsByStatusHandler(
    IReviewQueryService reviewQueryService,
    ILogger<GetReviewsByStatusHandler> logger)
    : IQueryHandler<GetReviewsByStatusQuery, PaginatedResult<ProductReviewDto>>
{
    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Pending", "Approved", "Rejected", "All" };

    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetReviewsByStatusQuery request, CancellationToken cancellationToken)
    {
        var normalized = string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status.Trim();
        if (!AllowedStatuses.Contains(normalized))
        {
            return ServiceResult<PaginatedResult<ProductReviewDto>>.Validation(
                "پارامتر status نامعتبر است. مقادیر مجاز: Pending، Approved، Rejected، All.");
        }

        var canonical = AllowedStatuses.First(s => s.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        try
        {
            var result = await reviewQueryService.GetReviewsByStatusAsync(
                canonical,
                request.Page,
                request.PageSize,
                cancellationToken);

            return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
        }
        catch (NullReferenceException ex)
        {
            logger.LogError(ex, "Null projection error while loading admin reviews for status {Status}", canonical);
            return ServiceResult<PaginatedResult<ProductReviewDto>>.Unexpected("خطا در بارگذاری فهرست نظرات ادمین.");
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database error while loading admin reviews for status {Status}", canonical);
            return ServiceResult<PaginatedResult<ProductReviewDto>>.Failure(
                Error.Infrastructure("خطا در ارتباط با پایگاه داده هنگام دریافت نظرات."));
        }
    }
}
