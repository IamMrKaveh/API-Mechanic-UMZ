using Application.Audit.Contracts;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Review.Interfaces;
using SharedKernel.Contracts;

namespace Application.Review.Features.Commands.DeleteReview;

public class DeleteReviewHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ILogger<DeleteReviewHandler> logger) : IRequestHandler<DeleteReviewCommand, ServiceResult>
{
    private readonly IReviewRepository _reviewRepository = reviewRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<DeleteReviewHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        DeleteReviewCommand request,
        CancellationToken ct)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review == null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        review.Delete(_currentUserService.CurrentUser.UserId);

        _reviewRepository.Update(review);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            review.ProductId,
            "DeleteReview",
            $"Review {request.ReviewId} soft-deleted.",
            _currentUserService.CurrentUser.UserId);

        _logger.LogInformation("Review {ReviewId} deleted by user {UserId}",
            request.ReviewId, _currentUserService.CurrentUser.UserId);

        return ServiceResult.Success();
    }
}