namespace Presentation.Wallet.Requests;

public sealed class AdminWalletAdjustmentRequestValidator : AbstractValidator<AdminWalletAdjustmentRequest>
{
    private const decimal MaxAmount = 1_000_000_000m;
    private const int ReasonMinLength = 3;
    private const int ReasonMaxLength = 500;
    private const int DescriptionMaxLength = 1000;

    public AdminWalletAdjustmentRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("مبلغ باید بزرگ‌تر از صفر باشد.")
            .LessThanOrEqualTo(MaxAmount)
            .WithMessage($"مبلغ نمی‌تواند از {MaxAmount:N0} بیشتر باشد.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("دلیل الزامی است.")
            .MinimumLength(ReasonMinLength)
            .WithMessage($"دلیل باید حداقل {ReasonMinLength} کاراکتر باشد.")
            .MaximumLength(ReasonMaxLength)
            .WithMessage($"دلیل نمی‌تواند بیش از {ReasonMaxLength} کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(DescriptionMaxLength)
            .WithMessage($"توضیحات نمی‌تواند بیش از {DescriptionMaxLength} کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.TransactionType)
            .IsInEnum()
            .WithMessage("نوع تراکنش نامعتبر است.");
    }
}
