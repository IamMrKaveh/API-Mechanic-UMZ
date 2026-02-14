namespace Application.Order.Features.Commands.CreateShippingMethod;

public class CreateShippingMethodValidator : AbstractValidator<CreateShippingMethodCommand>
{
    public CreateShippingMethodValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinDeliveryDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxDeliveryDays).GreaterThanOrEqualTo(0);
    }
}