namespace Application.Product.Features.Commands.RejectReview;

public class RejectReviewValidator : AbstractValidator<RejectReviewCommand>
{
    public RejectReviewValidator()
    {
        RuleFor(x => x.ReviewId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("دلیل رد نباید بیش از ۵۰۰ کاراکتر باشد.");
    }
}