namespace Application.Review.Features.Commands.BulkOperation;

public sealed class BulkRejectReviewsValidator : AbstractValidator<BulkRejectReviewsCommand>
{
    public BulkRejectReviewsValidator()
    {
        RuleFor(x => x.ReviewIds)
            .NotEmpty().WithMessage("حداقل یک شناسه نظر باید ارسال شود.")
            .Must(ids => ids.Count <= 100)
            .WithMessage("در هر درخواست حداکثر ۱۰۰ نظر قابل پردازش است.");

        RuleForEach(x => x.ReviewIds)
            .NotEmpty().WithMessage("شناسه نظر نامعتبر است.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("دلیل رد الزامی است.")
            .MaximumLength(500).WithMessage("دلیل رد نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");
    }
}
