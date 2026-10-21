using Application.Common.Validation;

namespace Application.Review.Features.Commands.DeleteOwnReview;

public class DeleteOwnReviewValidator : AbstractValidator<DeleteOwnReviewCommand>
{

    public DeleteOwnReviewValidator()
    {
        this.RuleForRequiredId(x => x.ReviewId);
    }
}
