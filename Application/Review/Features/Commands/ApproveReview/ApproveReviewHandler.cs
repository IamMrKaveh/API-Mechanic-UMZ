using Application.Audit.Contracts;
using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Review.Interfaces;
using SharedKernel.Contracts;

namespace Application.Review.Features.Commands.ApproveReview;

public class ApproveReviewHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ILogger<ApproveReviewHandler> logger) : IRequestHandler<ApproveReviewCommand, ServiceResult>
{
    private readonly IReviewRepository _reviewRepository = reviewRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<ApproveReviewHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        ApproveReviewCommand request,
        CancellationToken ct)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review == null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        try
        {
            review.Approve();

            _reviewRepository.Update(review);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogProductEventAsync(
                review.ProductId,
                "ApproveReview",
                $"Review {request.ReviewId} approved.",
                _currentUserService.CurrentUser.UserId);

            _logger.LogInformation("Review {ReviewId} approved by admin {UserId}",
                request.ReviewId, _currentUserService.CurrentUser.UserId);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }
    }
}