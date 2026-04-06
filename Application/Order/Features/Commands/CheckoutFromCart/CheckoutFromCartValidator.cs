namespace Application.Order.Features.Commands.CheckoutFromCart;

public class CheckoutFromCartValidator : AbstractValidator<CheckoutFromCartCommand>
{
    public CheckoutFromCartValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.ShippingId).NotEmpty();
        RuleFor(x => x.AddressId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.IpAddress).NotEmpty();
        RuleFor(x => x.DiscountCode)
            .MaximumLength(50).When(x => x.DiscountCode is not null);
    }
}