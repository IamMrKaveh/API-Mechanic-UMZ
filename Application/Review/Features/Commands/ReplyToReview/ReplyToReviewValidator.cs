namespace Application.Review.Features.Commands.ReplyToReview;

public class ReplyToReviewValidator : AbstractValidator<ReplyToReviewCommand>
{
    public ReplyToReviewValidator()
    {
        RuleFor(x => x.ReviewId).GreaterThan(0);
        RuleFor(x => x.Reply)
            .NotEmpty().WithMessage("متن پاسخ الزامی است.")
            .MaximumLength(1000).WithMessage("متن پاسخ نباید بیش از ۱۰۰۰ کاراکتر باشد.");
    }
}