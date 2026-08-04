namespace Application.Review.Features.Commands.DislikeReview;

public sealed class DislikeReviewValidator : AbstractValidator<DislikeReviewCommand>
{
    public DislikeReviewValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("شناسه نظر الزامی است.");
    }
}
