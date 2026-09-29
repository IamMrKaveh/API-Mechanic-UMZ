using Application.Common.Validation;

namespace Application.Order.Features.Commands.ActivateOrderStatus;

public class ActivateOrderStatusValidator : AbstractValidator<ActivateOrderStatusCommand>
{
    public ActivateOrderStatusValidator()
    {
        this.RuleForRequiredId(x => x.Id, "شناسه وضعیت الزامی است.");
    }
}