namespace Application.Wallet.Features.Commands.RejectWalletDebit;

public sealed class RejectWalletDebitValidator : AbstractValidator<RejectWalletDebitCommand>
{
    private const int RejectionReasonMaxLength = 500;

    public RejectWalletDebitValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("شناسه درخواست الزامی است.");

        RuleFor(x => x.RejectionReason)
            .MaximumLength(RejectionReasonMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.RejectionReason));
    }
}
