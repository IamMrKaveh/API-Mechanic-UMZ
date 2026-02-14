namespace Application.Order.Features.Commands.CancelOrder;

public class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("OrderId is required.");

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("دلیل لغو سفارش الزامی است.")
            .MaximumLength(500);
    }
}