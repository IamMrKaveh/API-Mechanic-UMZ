using Application.Common.Validation;

namespace Application.Order.Features.Commands.DeleteOrderStatus;

public class DeleteOrderStatusValidator : AbstractValidator<DeleteOrderStatusCommand>
{

    public DeleteOrderStatusValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
