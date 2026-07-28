namespace Presentation.Wallet.Requests;

public sealed class AdminWalletAdjustmentRequestValidator : AbstractValidator<AdminWalletAdjustmentRequest>
{
    private const int ReasonMinLength = 3;
    private const int ReasonMaxLength = 500;
    private const int DescriptionMaxLength = 1000;
    private const decimal AmountMax = 1_000_000_000m;

    public AdminWalletAdjustmentRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0m)
                .WithName("مبلغ")
                .WithMessage("مبلغ باید بزرگ‌تر از صفر باشد.")
            .LessThanOrEqualTo(AmountMax)
                .WithName("مبلغ")
                .WithMessage("مبلغ از سقف مجاز عبور کرده است.");

        RuleFor(x => x.Reason)
            .NotEmpty()
                .WithName("دلیل")
                .WithMessage("دلیل الزامی است.")
            .MinimumLength(ReasonMinLength)
                .WithName("دلیل")
                .WithMessage($"دلیل باید حداقل {ReasonMinLength} کاراکتر باشد.")
            .MaximumLength(ReasonMaxLength)
                .WithName("دلیل")
                .WithMessage($"دلیل نباید بیش از {ReasonMaxLength} کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(DescriptionMaxLength)
                .WithName("توضیحات")
                .WithMessage($"توضیحات نباید بیش از {DescriptionMaxLength} کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
