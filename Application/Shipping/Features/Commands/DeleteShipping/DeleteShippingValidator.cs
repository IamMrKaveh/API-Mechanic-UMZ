using Application.Common.Validation;

namespace Application.Shipping.Features.Commands.DeleteShipping;

public class DeleteShippingValidator : AbstractValidator<DeleteShippingCommand>
{

    public DeleteShippingValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
