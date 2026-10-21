using Application.Common.Validation;

namespace Application.Wallet.Features.Commands.ApproveWithdrawal;

public class ApproveWithdrawalValidator : AbstractValidator<ApproveWithdrawalCommand>
{

    public ApproveWithdrawalValidator()
    {
        this.RuleForRequiredId(x => x.WithdrawalId);
    }
}
