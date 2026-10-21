using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.ToggleWarehouseActive;

public class ToggleWarehouseActiveValidator : AbstractValidator<ToggleWarehouseActiveCommand>
{

    public ToggleWarehouseActiveValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
