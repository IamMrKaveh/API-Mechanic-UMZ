using Application.Common.Validation;

namespace Application.Shipping.Features.Commands.SetDefaultShipping;

public class SetDefaultShippingValidator : AbstractValidator<SetDefaultShippingCommand>
{

    public SetDefaultShippingValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
