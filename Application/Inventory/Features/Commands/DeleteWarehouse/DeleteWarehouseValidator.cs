using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.DeleteWarehouse;

public class DeleteWarehouseValidator : AbstractValidator<DeleteWarehouseCommand>
{

    public DeleteWarehouseValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
