namespace Application.Product.Features.Commands.RejectReview;

public class RejectReviewHandler : IRequestHandler<RejectReviewCommand, ServiceResult>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RejectReviewHandler> _logger;

    public RejectReviewHandler(
        IReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<RejectReviewHandler> logger)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(RejectReviewCommand request, CancellationToken ct)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review == null)
            return ServiceResult.Failure("نظر یافت نشد.", 404);

        try
        {
            review.Reject(request.Reason);

            _reviewRepository.Update(review);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogProductEventAsync(
                review.ProductId,
                "RejectReview",
                $"Review {request.ReviewId} rejected. Reason: {request.Reason ?? "N/A"}",
                _currentUserService.UserId);

            _logger.LogInformation("Review {ReviewId} rejected by admin {UserId}",
                request.ReviewId, _currentUserService.UserId);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}