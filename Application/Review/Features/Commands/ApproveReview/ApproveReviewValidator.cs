namespace Application.Review.Features.Commands.ApproveReview;

public sealed class ApproveReviewValidator : AbstractValidator<ApproveReviewCommand>
{
    public ApproveReviewValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}
