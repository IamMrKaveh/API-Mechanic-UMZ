namespace Application.Wallet.Features.Commands.ReserveWallet;

public sealed class ReserveWalletValidator : AbstractValidator<ReserveWalletCommand>
{
    public ReserveWalletValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("شناسه کیف پول الزامی است.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("مبلغ رزرو باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(1_000_000_000m).WithMessage("مبلغ رزرو از سقف مجاز عبور کرده است.");

        RuleFor(x => x.ExpiresAt!.Value)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("زمان انقضای رزرو باید در آینده باشد.");
    }
}