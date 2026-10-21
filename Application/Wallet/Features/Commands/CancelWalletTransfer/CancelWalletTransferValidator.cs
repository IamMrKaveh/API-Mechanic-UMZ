using Application.Common.Validation;

namespace Application.Wallet.Features.Commands.CancelWalletTransfer;

public class CancelWalletTransferValidator : AbstractValidator<CancelWalletTransferCommand>
{

    public CancelWalletTransferValidator()
    {
        this.RuleForRequiredId(x => x.TransferId);
    }
}
