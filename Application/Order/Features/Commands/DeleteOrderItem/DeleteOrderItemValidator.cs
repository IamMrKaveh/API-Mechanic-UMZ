using Application.Common.Validation;

namespace Application.Order.Features.Commands.DeleteOrderItem;

public class DeleteOrderItemValidator : AbstractValidator<DeleteOrderItemCommand>
{

    public DeleteOrderItemValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
