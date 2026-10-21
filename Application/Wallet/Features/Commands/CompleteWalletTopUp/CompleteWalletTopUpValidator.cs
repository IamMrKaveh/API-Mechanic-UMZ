using Application.Common.Validation;

namespace Application.Wallet.Features.Commands.CompleteWalletTopUp;

public class CompleteWalletTopUpValidator : AbstractValidator<CompleteWalletTopUpCommand>
{

    public CompleteWalletTopUpValidator()
    {
        RuleFor(x => x.Authority).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
    }
}
