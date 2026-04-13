namespace Application.Order.Features.Commands.UpdateOrderStatus;

public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.");
        RuleFor(x => x.NewStatus).NotEmpty().WithMessage("New status is required.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion is required.");
    }
}