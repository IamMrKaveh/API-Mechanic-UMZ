using Application.Common.Validation;

namespace Application.Wallet.Features.Commands.CancelWithdrawal;

public class CancelWithdrawalValidator : AbstractValidator<CancelWithdrawalCommand>
{

    public CancelWithdrawalValidator()
    {
        this.RuleForRequiredId(x => x.WithdrawalId);
    }
}
