using Application.Common.Validation;

namespace Application.Review.Features.Commands.ReplyToReview;

public sealed class ReplyToReviewValidator : AbstractValidator<ReplyToReviewCommand>
{
    public ReplyToReviewValidator()
    {
        this.RuleForRequiredId(x => x.ReviewId, "شناسه نظر الزامی است.");

        RuleFor(x => x.Reply)
            .NotEmpty().WithMessage("متن پاسخ الزامی است.")
            .MaximumLength(1000).WithMessage("متن پاسخ نباید بیش از ۱۰۰۰ کاراکتر باشد.");
    }
}