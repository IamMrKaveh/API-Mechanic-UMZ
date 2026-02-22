namespace Application.Review.Features.Commands.UpdateReviewStatus;

public class UpdateReviewStatusHandler : IRequestHandler<UpdateReviewStatusCommand, ServiceResult>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateReviewStatusHandler> _logger;

    public UpdateReviewStatusHandler(IReviewRepository reviewRepository, IUnitOfWork unitOfWork, ILogger<UpdateReviewStatusHandler> logger)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(UpdateReviewStatusCommand request, CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId);
        if (review == null)
        {
            return ServiceResult.Failure("Review not found");
        }

        if (request.Status == "Approved")
            review.Approve();
        else if (request.Status == "Rejected")
            review.Reject();

        _reviewRepository.Update(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Review {ReviewId} status updated to {Status}", request.ReviewId, request.Status);
        return ServiceResult.Success();
    }
}