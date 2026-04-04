using Application.Audit.Contracts;
using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Review.Interfaces;
using SharedKernel.Contracts;

namespace Application.Review.Features.Commands.ReplyToReview;

public class ReplyToReviewHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ILogger<ReplyToReviewHandler> logger) : IRequestHandler<ReplyToReviewCommand, ServiceResult>
{
    private readonly IReviewRepository _reviewRepository = reviewRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<ReplyToReviewHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        ReplyToReviewCommand request,
        CancellationToken ct)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review == null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        try
        {
            review.AddAdminReply(request.Reply);

            _reviewRepository.Update(review);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogProductEventAsync(
                review.ProductId,
                "ReplyToReview",
                $"Admin replied to review {request.ReviewId}.",
                _currentUserService.CurrentUser.UserId);

            _logger.LogInformation("Admin {UserId} replied to review {ReviewId}",
                _currentUserService.CurrentUser.UserId, request.ReviewId);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }
    }
}