using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.CreateWarehouse;

public class CreateWarehouseValidator : AbstractValidator<CreateWarehouseCommand>
{

    public CreateWarehouseValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9\\-_]+$")
            .WithMessage("کد انبار باید فقط شامل حروف انگلیسی بزرگ، عدد، خط تیره یا زیرخط باشد.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
    }
}
