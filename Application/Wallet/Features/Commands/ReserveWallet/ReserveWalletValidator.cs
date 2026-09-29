using Application.Common.Validation;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wallet.Features.Commands.ReserveWallet;

public sealed class ReserveWalletValidator : AbstractValidator<ReserveWalletCommand>
{
    public ReserveWalletValidator(IDateTimeProvider dateTimeProvider)
    {
        this.RuleForRequiredId(x => x.UserId, "شناسه کاربر الزامی است.");

        this.RuleForRequiredId(x => x.WalletId, "شناسه کیف پول الزامی است.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("مبلغ رزرو باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(1_000_000_000m).WithMessage("مبلغ رزرو از سقف مجاز عبور کرده است.");

        RuleFor(x => x.ExpiresAt)
            .Must(value => value!.Value > dateTimeProvider.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("زمان انقضای رزرو باید در آینده باشد.");
    }
}
