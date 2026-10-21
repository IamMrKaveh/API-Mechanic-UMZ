using Application.Common.Validation;

namespace Application.Payment.Features.Commands.DeletePaymentMethod;

public class DeletePaymentMethodValidator : AbstractValidator<DeletePaymentMethodCommand>
{

    public DeletePaymentMethodValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
