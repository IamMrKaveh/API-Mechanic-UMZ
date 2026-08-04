namespace Application.Review.Features.Commands.LikeReview;

public sealed class LikeReviewValidator : AbstractValidator<LikeReviewCommand>
{
    public LikeReviewValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("شناسه نظر الزامی است.");
    }
}
