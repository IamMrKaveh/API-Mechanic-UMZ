namespace Application.Wallet.Features.Commands.ApproveWalletDebit;

public sealed class ApproveWalletDebitValidator : AbstractValidator<ApproveWalletDebitCommand>
{
    public ApproveWalletDebitValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("شناسه درخواست الزامی است.");
    }
}
