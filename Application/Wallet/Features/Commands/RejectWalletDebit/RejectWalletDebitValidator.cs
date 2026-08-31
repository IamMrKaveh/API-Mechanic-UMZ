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
            .NotEmpty()
                .WithMessage("دلیل رد درخواست الزامی است.")
            .MaximumLength(RejectionReasonMaxLength)
                .WithMessage($"دلیل رد درخواست نمی‌تواند بیش از {RejectionReasonMaxLength} کاراکتر باشد.");
    }
}
