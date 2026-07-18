namespace Application.Wallet.Features.Commands.FreezeWallet;

public sealed class FreezeWalletValidator : AbstractValidator<FreezeWalletCommand>
{
    public FreezeWalletValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("دلیل مسدودسازی الزامی است.")
            .MaximumLength(500)
            .WithMessage("طول دلیل مسدودسازی نباید بیش از 500 کاراکتر باشد.");
    }
}