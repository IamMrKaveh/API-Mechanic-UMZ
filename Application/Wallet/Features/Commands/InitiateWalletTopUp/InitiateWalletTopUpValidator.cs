namespace Application.Wallet.Features.Commands.InitiateWalletTopUp;

public sealed class InitiateWalletTopUpValidator : AbstractValidator<InitiateWalletTopUpCommand>
{
    public InitiateWalletTopUpValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(10_000m)
                .WithMessage("حداقل مبلغ شارژ ۱۰,۰۰۰ تومان است.")
            .LessThanOrEqualTo(1_000_000_000m)
                .WithMessage("مبلغ از سقف مجاز عبور کرده است.");

        RuleFor(x => x.Gateway)
            .NotEmpty().WithMessage("درگاه پرداخت الزامی است.")
            .MaximumLength(64);
    }
}