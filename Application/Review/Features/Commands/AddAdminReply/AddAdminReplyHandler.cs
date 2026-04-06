using Application.Common.Results;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Review.Features.Commands.AddAdminReply;

public class AddAdminReplyHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AddAdminReplyCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(AddAdminReplyCommand request, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(ProductReviewId.From(request.ReviewId), ct);
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