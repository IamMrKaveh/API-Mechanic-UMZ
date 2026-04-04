using Application.Audit.Contracts;
using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Review.Interfaces;
using SharedKernel.Contracts;

namespace Application.Review.Features.Commands.RejectReview;

public class RejectReviewHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ILogger<RejectReviewHandler> logger) : IRequestHandler<RejectReviewCommand, ServiceResult>
{
    private readonly IReviewRepository _reviewRepository = reviewRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<RejectReviewHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        RejectReviewCommand request,
        CancellationToken ct)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review == null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        try
        {
            review.Reject(request.Reason);

            _reviewRepository.Update(review);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogProductEventAsync(
                review.ProductId,
                "RejectReview",
                $"Review {request.ReviewId} rejected. Reason: {request.Reason ?? "N/A"}",
                _currentUserService.CurrentUser.UserId);

            _logger.LogInformation("Review {ReviewId} rejected by admin {UserId}",
                request.ReviewId, _currentUserService.CurrentUser.UserId);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }
    }
}