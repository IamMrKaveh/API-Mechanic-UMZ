namespace Application.Review.Features.Commands.BulkOperation;

public sealed class BulkDeleteReviewsValidator : AbstractValidator<BulkDeleteReviewsCommand>
{
    public BulkDeleteReviewsValidator()
    {
        RuleFor(x => x.ReviewIds)
            .NotEmpty().WithMessage("حداقل یک شناسه نظر باید ارسال شود.")
            .Must(ids => ids.Count <= 100)
            .WithMessage("در هر درخواست حداکثر ۱۰۰ نظر قابل پردازش است.");

        RuleForEach(x => x.ReviewIds)
            .NotEmpty().WithMessage("شناسه نظر نامعتبر است.");

        RuleFor(x => x.Reason!)
            .MaximumLength(500).WithMessage("دلیل حذف نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
