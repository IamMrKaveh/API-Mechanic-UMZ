using Application.Common.Validation;

namespace Application.Payment.Features.Commands.ActivatePaymentMethod;

public class ActivatePaymentMethodValidator : AbstractValidator<ActivatePaymentMethodCommand>
{

    public ActivatePaymentMethodValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
