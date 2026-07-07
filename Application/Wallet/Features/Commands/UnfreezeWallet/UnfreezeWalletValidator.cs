namespace Application.Wallet.Features.Commands.UnfreezeWallet;

public sealed class UnfreezeWalletValidator : AbstractValidator<UnfreezeWalletCommand>
{
    public UnfreezeWalletValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.AdminId)
            .NotEmpty()
            .WithMessage("شناسه ادمین الزامی است.");
    }
}