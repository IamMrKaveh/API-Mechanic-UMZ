namespace Application.Review.Features.Commands.DeleteReview;

public class DeleteReviewHandler : IRequestHandler<DeleteReviewCommand, ServiceResult>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteReviewHandler> _logger;

    public DeleteReviewHandler(
        IReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<DeleteReviewHandler> logger)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(DeleteReviewCommand request, CancellationToken ct)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review == null)
            return ServiceResult.Failure("نظر یافت نشد.", 404);

        review.Delete(_currentUserService.UserId);

        _reviewRepository.Update(review);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            review.ProductId,
            "DeleteReview",
            $"Review {request.ReviewId} soft-deleted.",
            _currentUserService.UserId);

        _logger.LogInformation("Review {ReviewId} deleted by user {UserId}",
            request.ReviewId, _currentUserService.UserId);

        return ServiceResult.Success();
    }
}