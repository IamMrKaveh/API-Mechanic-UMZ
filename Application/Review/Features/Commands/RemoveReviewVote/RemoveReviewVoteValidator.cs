namespace Application.Review.Features.Commands.RemoveReviewVote;

public sealed class RemoveReviewVoteValidator : AbstractValidator<RemoveReviewVoteCommand>
{
    public RemoveReviewVoteValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("شناسه نظر الزامی است.");
    }
}
