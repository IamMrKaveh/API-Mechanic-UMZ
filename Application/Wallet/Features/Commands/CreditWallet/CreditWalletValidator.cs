namespace Application.Wallet.Features.Commands.CreditWallet;

public sealed class CreditWalletValidator : AbstractValidator<CreditWalletCommand>
{
    private const int IdempotencyKeyMaxLength = 128;
    private const int DescriptionMaxLength = 1000;
    private const decimal AmountMax = 1_000_000_000m;

    public CreditWalletValidator()
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

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
                .WithName("کلید یکتای درخواست")
                .WithMessage("کلید یکتای درخواست الزامی است.")
            .MaximumLength(IdempotencyKeyMaxLength)
                .WithName("کلید یکتای درخواست")
                .WithMessage($"طول کلید یکتا نباید بیش از {IdempotencyKeyMaxLength} کاراکتر باشد.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty()
                .WithName("شناسه مرجع")
                .WithMessage("شناسه مرجع الزامی است.")
            .MaximumLength(256)
                .WithName("شناسه مرجع");

        RuleFor(x => x.TransactionType)
            .IsInEnum()
                .WithName("نوع تراکنش")
                .WithMessage("نوع تراکنش نامعتبر است.");

        RuleFor(x => x.ReferenceType)
            .IsInEnum()
                .WithName("نوع مرجع")
                .WithMessage("نوع مرجع نامعتبر است.");

        RuleFor(x => x.CorrelationId)
            .MaximumLength(IdempotencyKeyMaxLength)
                .WithName("شناسه همبستگی")
            .When(x => !string.IsNullOrWhiteSpace(x.CorrelationId));

        RuleFor(x => x.Description)
            .MaximumLength(DescriptionMaxLength)
                .WithName("توضیحات")
                .WithMessage($"توضیحات نباید بیش از {DescriptionMaxLength} کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
