using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.ReverseInventoryTransaction;

public class ReverseInventoryValidator : AbstractValidator<ReverseInventoryCommand>
{
    public ReverseInventoryValidator()
    {
        this.RuleForRequiredId(x => x.VariantId);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
