using Application.Common.Validation;

namespace Application.Order.Features.Commands.ApproveReturn;

public class ApproveReturnValidator : AbstractValidator<ApproveReturnCommand>
{

    public ApproveReturnValidator()
    {
        this.RuleForRequiredId(x => x.OrderId);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
