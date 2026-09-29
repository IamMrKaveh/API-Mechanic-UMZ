using Application.Common.Validation;

namespace Application.Order.Features.Commands.SetDefaultOrderStatus;

public class SetDefaultOrderStatusValidator : AbstractValidator<SetDefaultOrderStatusCommand>
{
    public SetDefaultOrderStatusValidator()
    {
        this.RuleForRequiredId(x => x.Id, "شناسه وضعیت الزامی است.");
    }
}