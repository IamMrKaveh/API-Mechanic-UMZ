namespace Application.Shipping.Features.Commands.CreateShipping;

public class CreateShippingValidator : AbstractValidator<CreateShippingCommand>
{
    public CreateShippingValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinDeliveryDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxDeliveryDays).GreaterThanOrEqualTo(0);
    }
}