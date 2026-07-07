namespace Application.Wallet.Features.Commands.MarkWithdrawalPaid;

public sealed class MarkWithdrawalPaidValidator : AbstractValidator<MarkWithdrawalPaidCommand>
{
    public MarkWithdrawalPaidValidator()
    {
        RuleFor(x => x.WithdrawalId).NotEmpty();
        RuleFor(x => x.AdminId).NotEmpty();
        RuleFor(x => x.BankReferenceNumber)
            .NotEmpty().WithMessage("شماره پیگیری بانکی الزامی است.")
            .MaximumLength(64).WithMessage("طول شماره پیگیری نامعتبر است.");
    }
}