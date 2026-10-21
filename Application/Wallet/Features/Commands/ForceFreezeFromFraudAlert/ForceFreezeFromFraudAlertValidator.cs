using Application.Common.Validation;

namespace Application.Wallet.Features.Commands.ForceFreezeFromFraudAlert;

public class ForceFreezeFromFraudAlertValidator : AbstractValidator<ForceFreezeFromFraudAlertCommand>
{

    public ForceFreezeFromFraudAlertValidator()
    {
        this.RuleForRequiredId(x => x.AlertId);
        RuleFor(x => x.AdditionalNote).MaximumLength(500);
    }
}
