using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.SetDefaultWarehouse;

public class SetDefaultWarehouseValidator : AbstractValidator<SetDefaultWarehouseCommand>
{

    public SetDefaultWarehouseValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
