namespace Application.Review.Features.Commands.ReplyToReview;

public class ReplyToReviewHandler : IRequestHandler<ReplyToReviewCommand, ServiceResult>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ReplyToReviewHandler> _logger;

    public ReplyToReviewHandler(
        IReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<ReplyToReviewHandler> logger)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(ReplyToReviewCommand request, CancellationToken ct)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review == null)
            return ServiceResult.Failure("نظر یافت نشد.", 404);

        try
        {
            review.AddAdminReply(request.Reply);

            _reviewRepository.Update(review);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogProductEventAsync(
                review.ProductId,
                "ReplyToReview",
                $"Admin replied to review {request.ReviewId}.",
                _currentUserService.UserId);

            _logger.LogInformation("Admin {UserId} replied to review {ReviewId}",
                _currentUserService.UserId, request.ReviewId);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}