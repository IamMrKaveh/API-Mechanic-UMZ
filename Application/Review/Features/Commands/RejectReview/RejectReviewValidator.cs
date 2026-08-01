namespace Application.Review.Features.Commands.RejectReview;

public class RejectReviewValidator : AbstractValidator<RejectReviewCommand>
{
    public RejectReviewValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("دلیل رد الزامی است.")
            .MaximumLength(500).WithMessage("دلیل رد نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");
    }
}
