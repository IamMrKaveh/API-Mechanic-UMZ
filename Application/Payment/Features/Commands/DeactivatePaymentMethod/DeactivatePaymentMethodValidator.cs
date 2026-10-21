using Application.Common.Validation;

namespace Application.Payment.Features.Commands.DeactivatePaymentMethod;

public class DeactivatePaymentMethodValidator : AbstractValidator<DeactivatePaymentMethodCommand>
{

    public DeactivatePaymentMethodValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
