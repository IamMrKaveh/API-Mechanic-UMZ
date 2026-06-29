namespace Application.Order.Features.Commands.SetDefaultOrderStatus;

public class SetDefaultOrderStatusValidator : AbstractValidator<SetDefaultOrderStatusCommand>
{
    public SetDefaultOrderStatusValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("شناسه وضعیت الزامی است.");
    }
}