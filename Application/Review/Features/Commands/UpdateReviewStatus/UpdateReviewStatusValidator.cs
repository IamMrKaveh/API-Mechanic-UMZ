namespace Application.Review.Features.Commands.UpdateReviewStatus;

public sealed class UpdateReviewStatusValidator : AbstractValidator<UpdateReviewStatusCommand>
{
    private static readonly string[] AllowedStatuses = { "Approved", "Rejected" };

    public UpdateReviewStatusValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("مقدار Status الزامی است.")
            .Must(s => AllowedStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("مقدار Status نامعتبر است. مقادیر مجاز: Approved، Rejected.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("در صورت رد نظر، ذکر دلیل الزامی است.")
            .When(x => string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase),
                  ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("دلیل رد نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason),
                  ApplyConditionTo.CurrentValidator);
    }
}
