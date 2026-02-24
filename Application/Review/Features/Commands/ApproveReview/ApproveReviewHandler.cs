namespace Application.Review.Features.Commands.ApproveReview;

public class ApproveReviewHandler : IRequestHandler<ApproveReviewCommand, ServiceResult>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ApproveReviewHandler> _logger;

    public ApproveReviewHandler(
        IReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<ApproveReviewHandler> logger)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(ApproveReviewCommand request, CancellationToken ct)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review == null)
            return ServiceResult.Failure("نظر یافت نشد.", 404);

        try
        {
            review.Approve();

            _reviewRepository.Update(review);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogProductEventAsync(
                review.ProductId,
                "ApproveReview",
                $"Review {request.ReviewId} approved.",
                _currentUserService.UserId);

            _logger.LogInformation("Review {ReviewId} approved by admin {UserId}",
                request.ReviewId, _currentUserService.UserId);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}