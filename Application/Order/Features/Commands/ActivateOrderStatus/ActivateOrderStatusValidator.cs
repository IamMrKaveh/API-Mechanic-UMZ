namespace Application.Order.Features.Commands.ActivateOrderStatus;

public class ActivateOrderStatusValidator : AbstractValidator<ActivateOrderStatusCommand>
{
    public ActivateOrderStatusValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("شناسه وضعیت الزامی است.");
    }
}