namespace Application.Shipping.Features.Commands.UpdateShipping;

public class UpdateShippingValidator : AbstractValidator<UpdateShippingCommand>
{
    public UpdateShippingValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BaseCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinDeliveryDays).GreaterThan(0);
        RuleFor(x => x.MaxDeliveryDays).GreaterThanOrEqualTo(x => x.MinDeliveryDays);
    }
}