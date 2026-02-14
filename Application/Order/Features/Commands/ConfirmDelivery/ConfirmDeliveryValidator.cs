namespace Application.Order.Features.Commands.ConfirmDelivery;

public class ConfirmDeliveryValidator : AbstractValidator<ConfirmDeliveryCommand>
{
    public ConfirmDeliveryValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("OrderId is required.");
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("UserId is required.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion is required for concurrency control.");
    }
}