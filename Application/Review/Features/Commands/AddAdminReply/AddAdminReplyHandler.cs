using Domain.Common.Exceptions;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.AddAdminReply;

public class AddAdminReplyHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AddAdminReplyCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(AddAdminReplyCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        try
        {
            review.AddAdminReply(request.Reply);
            reviewRepository.Update(review);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}