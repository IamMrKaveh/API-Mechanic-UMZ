namespace Application.Wallet.Features.Commands.RequestWalletDebit;

public sealed class RequestWalletDebitValidator : AbstractValidator<RequestWalletDebitCommand>
{
    private const int IdempotencyKeyMaxLength = 128;
    private const int ReasonMaxLength = 500;
    private const int DescriptionMaxLength = 1000;
    private const decimal AmountMax = 1_000_000_000m;
    private const int MinExpiryHours = 1;
    private const int MaxExpiryHours = 168;

    public RequestWalletDebitValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
                .WithName("شناسه کاربر")
                .WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.Amount)
            .GreaterThan(0m)
                .WithName("مبلغ")
                .WithMessage("مبلغ باید بزرگ‌تر از صفر باشد.")
            .LessThanOrEqualTo(AmountMax)
                .WithName("مبلغ")
                .WithMessage("مبلغ از سقف مجاز عبور کرده است.");

        RuleFor(x => x.Reason)
            .NotEmpty()
                .WithName("دلیل درخواست")
                .WithMessage("دلیل درخواست الزامی است.")
            .MaximumLength(ReasonMaxLength)
                .WithName("دلیل درخواست")
                .WithMessage($"دلیل نباید بیش از {ReasonMaxLength} کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(DescriptionMaxLength)
                .WithName("توضیحات")
                .WithMessage($"توضیحات نباید بیش از {DescriptionMaxLength} کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
                .WithName("کلید یکتا")
                .WithMessage("کلید یکتای درخواست الزامی است.")
            .MaximumLength(IdempotencyKeyMaxLength);

        RuleFor(x => x.ExpiryHours)
            .InclusiveBetween(MinExpiryHours, MaxExpiryHours)
                .WithName("مدت انقضا")
                .WithMessage($"مدت انقضا باید بین {MinExpiryHours} تا {MaxExpiryHours} ساعت باشد.");
    }
}
