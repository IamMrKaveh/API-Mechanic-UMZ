using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.UpdateWarehouse;

public class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseCommand>
{

    public UpdateWarehouseValidator()
    {
        this.RuleForRequiredId(x => x.Id);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
    }
}
