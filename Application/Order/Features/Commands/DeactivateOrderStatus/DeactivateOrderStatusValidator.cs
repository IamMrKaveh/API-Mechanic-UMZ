using Application.Common.Validation;

namespace Application.Order.Features.Commands.DeactivateOrderStatus;

public class DeactivateOrderStatusValidator : AbstractValidator<DeactivateOrderStatusCommand>
{
    public DeactivateOrderStatusValidator()
    {
        this.RuleForRequiredId(x => x.Id, "شناسه وضعیت الزامی است.");
    }
}