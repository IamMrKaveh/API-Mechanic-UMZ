using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.BulkOperation;

public sealed class BulkApproveReviewsHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<BulkApproveReviewsCommand, BulkOperationResult>
{
    public async Task<ServiceResult<BulkOperationResult>> Handle(
        BulkApproveReviewsCommand request, CancellationToken ct)
    {
        return await unitOfWork.ExecuteStrategyAsync(async token =>
        {
            var failures = new List<BulkOperationFailure>();
            var successCount = 0;

            foreach (var id in request.ReviewIds.Distinct())
            {
                try
                {
                    var reviewId = ReviewId.From(id);
                    var review = await reviewRepository.GetByIdAsync(reviewId, token);
                    if (review is null)
                    {
                        failures.Add(new BulkOperationFailure(id, "نظر یافت نشد."));
                        continue;
                    }

                    review.Approve();
                    reviewRepository.Update(review);
                    successCount++;
                }
                catch (DomainException ex)
                {
                    failures.Add(new BulkOperationFailure(id, ex.Message));
                }
            }

            var result = new BulkOperationResult(
                successCount,
                failures.Count,
                failures.Select(f => f.ReviewId).ToList(),
                failures);

            return ServiceResult<BulkOperationResult>.Success(result);
        }, ct);
    }
}
