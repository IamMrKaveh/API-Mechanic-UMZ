namespace Application.Review.Features.Commands.RestoreReview;

public sealed class RestoreReviewValidator : AbstractValidator<RestoreReviewCommand>
{
    public RestoreReviewValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}
