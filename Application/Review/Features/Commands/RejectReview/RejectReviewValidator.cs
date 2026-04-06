namespace Application.Review.Features.Commands.RejectReview;

public class RejectReviewValidator : AbstractValidator<RejectReviewCommand>
{
    public RejectReviewValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason is not null);
    }
}