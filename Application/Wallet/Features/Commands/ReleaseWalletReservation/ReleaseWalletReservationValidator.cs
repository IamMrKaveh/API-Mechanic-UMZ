namespace Application.Wallet.Features.Commands.ReleaseWalletReservation;

public sealed class ReleaseWalletReservationValidator : AbstractValidator<ReleaseWalletReservationCommand>
{
    public ReleaseWalletReservationValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.WalletReservationId)
            .NotEmpty().WithMessage("شناسه رزرو کیف پول الزامی است.");
    }
}