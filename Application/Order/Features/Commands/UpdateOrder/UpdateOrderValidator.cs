using Application.Common.Validation;

namespace Application.Order.Features.Commands.UpdateOrder;

public class UpdateOrderValidator : AbstractValidator<UpdateOrderCommand>
{

    public UpdateOrderValidator()
    {
        this.RuleForRequiredId(x => x.OrderId);
        RuleFor(x => x.Dto).NotNull();
        RuleFor(x => x.RowVersion).MaximumLength(64);
    }
}
