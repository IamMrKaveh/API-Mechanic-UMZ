using Application.Common.Validation;

namespace Application.Cart.Features.Commands.RemoveItemFromCart;

public class RemoveItemFromCartValidator : AbstractValidator<RemoveItemFromCartCommand>
{

    public RemoveItemFromCartValidator()
    {
        this.RuleForRequiredId(x => x.VariantId);
    }
}
