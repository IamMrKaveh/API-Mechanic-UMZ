namespace Application.Wallet.Features.Commands.CreditWallet;

public sealed class CreditWalletValidator : AbstractValidator<CreditWalletCommand>
{
    public CreditWalletValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("مبلغ باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(1_000_000_000m).WithMessage("مبلغ از سقف مجاز عبور کرده است.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("کلید یکتای درخواست الزامی است.")
            .MaximumLength(128).WithMessage("طول کلید یکتا نباید بیش از ۱۲۸ کاراکتر باشد.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty().WithMessage("شناسه مرجع الزامی است.")
            .MaximumLength(256);

        RuleFor(x => x.TransactionType)
            .IsInEnum().WithMessage("نوع تراکنش نامعتبر است.");

        RuleFor(x => x.ReferenceType)
            .IsInEnum().WithMessage("نوع مرجع نامعتبر است.");

        RuleFor(x => x.CorrelationId)
            .MaximumLength(128)
            .When(x => !string.IsNullOrWhiteSpace(x.CorrelationId));

        RuleFor(x => x.Description)
            .MaximumLength(512)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}