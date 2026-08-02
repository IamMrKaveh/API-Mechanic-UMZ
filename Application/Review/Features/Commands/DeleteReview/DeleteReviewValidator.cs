namespace Application.Review.Features.Commands.DeleteReview;

public sealed class DeleteReviewValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();

        RuleFor(x => x.Reason!)
            .MaximumLength(500).WithMessage("دلیل حذف نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
