using Application.Common.Validation;

namespace Application.Wallet.Features.Commands.ReleaseWalletReservation;

public sealed class ReleaseWalletReservationValidator : AbstractValidator<ReleaseWalletReservationCommand>
{
    public ReleaseWalletReservationValidator()
    {
        this.RuleForRequiredId(x => x.UserId, "شناسه کاربر الزامی است.");

        this.RuleForRequiredId(x => x.WalletReservationId, "شناسه رزرو کیف پول الزامی است.");
    }
}