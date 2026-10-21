using Application.Common.Validation;

namespace Application.Shipping.Features.Commands.RestoreShipping;

public class RestoreShippingValidator : AbstractValidator<RestoreShippingCommand>
{

    public RestoreShippingValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
