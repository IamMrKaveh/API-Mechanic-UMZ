namespace Application.Order.Features.Commands.DeactivateOrderStatus;

public class DeactivateOrderStatusValidator : AbstractValidator<DeactivateOrderStatusCommand>
{
    public DeactivateOrderStatusValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("شناسه وضعیت الزامی است.");
    }
}