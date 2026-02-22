namespace Application.Order.Features.Commands.CheckoutFromCart;

public class CheckoutFromCartValidator : AbstractValidator<CheckoutFromCartCommand>
{
    public CheckoutFromCartValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId is required.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("IdempotencyKey is required.")
            .MaximumLength(256);

        RuleFor(x => x.ShippingId)
            .GreaterThan(0)
            .WithMessage("Shipping is required.");

        When(x => !x.UserAddressId.HasValue, () =>
        {
            RuleFor(x => x.NewAddress)
                .NotNull()
                .WithMessage("Either existing address or new address is required.");

            When(x => x.NewAddress != null, () =>
            {
                RuleFor(x => x.NewAddress!.ReceiverName)
                    .NotEmpty()
                    .MaximumLength(100);

                RuleFor(x => x.NewAddress!.PhoneNumber)
                    .NotEmpty()
                    .Matches(@"^09\d{9}$")
                    .WithMessage("Invalid phone number format.");

                RuleFor(x => x.NewAddress!.Province)
                    .NotEmpty()
                    .MaximumLength(50);

                RuleFor(x => x.NewAddress!.City)
                    .NotEmpty()
                    .MaximumLength(50);

                RuleFor(x => x.NewAddress!.Address)
                    .NotEmpty()
                    .MaximumLength(500);

                RuleFor(x => x.NewAddress!.PostalCode)
                    .NotEmpty()
                    .Matches(@"^\d{10}$")
                    .WithMessage("Postal code must be 10 digits.");
            });
        });

        RuleFor(x => x.DiscountCode)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.DiscountCode));
    }
}