using Domain.Review.Interfaces;

namespace Application.Review.Features.Commands.UpdateReviewStatus;

public class UpdateReviewStatusHandler(IReviewRepository reviewRepository, IUnitOfWork unitOfWork, ILogger<UpdateReviewStatusHandler> logger) : IRequestHandler<UpdateReviewStatusCommand, ServiceResult>
{
    private readonly IReviewRepository _reviewRepository = reviewRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<UpdateReviewStatusHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        UpdateReviewStatusCommand request,
        CancellationToken ct)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review == null)
        {
            return ServiceResult.NotFound("Review not found");
        }

        if (request.Status == "Approved")
            review.Approve();
        else if (request.Status == "Rejected")
            review.Reject();

        _reviewRepository.Update(review);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Review {ReviewId} status updated to {Status}", request.ReviewId, request.Status);
        return ServiceResult.Success();
    }
}