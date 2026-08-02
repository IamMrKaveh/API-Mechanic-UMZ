namespace Application.Review.Features.Commands.BulkOperation;

public sealed class BulkApproveReviewsValidator : AbstractValidator<BulkApproveReviewsCommand>
{
    public BulkApproveReviewsValidator()
    {
        RuleFor(x => x.ReviewIds)
            .NotEmpty().WithMessage("حداقل یک شناسه نظر باید ارسال شود.")
            .Must(ids => ids.Count <= 100)
            .WithMessage("در هر درخواست حداکثر ۱۰۰ نظر قابل پردازش است.");

        RuleForEach(x => x.ReviewIds)
            .NotEmpty().WithMessage("شناسه نظر نامعتبر است.");
    }
}
