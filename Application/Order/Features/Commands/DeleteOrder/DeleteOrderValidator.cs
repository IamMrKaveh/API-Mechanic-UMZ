using Application.Common.Validation;

namespace Application.Order.Features.Commands.DeleteOrder;

public class DeleteOrderValidator : AbstractValidator<DeleteOrderCommand>
{

    public DeleteOrderValidator()
    {
        this.RuleForRequiredId(x => x.OrderId);
    }
}
