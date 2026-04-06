namespace Application.Order.Features.Commands.ConfirmDelivery;

public class ConfirmDeliveryValidator : AbstractValidator<ConfirmDeliveryCommand>
{
    public ConfirmDeliveryValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}