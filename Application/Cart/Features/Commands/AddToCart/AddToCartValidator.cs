namespace Application.Cart.Features.Commands.AddToCart;

public class AddToCartValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.GuestToken))
            .WithMessage("UserId یا GuestToken الزامی است.");

        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("شناسه محصول الزامی است.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("تعداد باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(100).WithMessage("تعداد نمی‌تواند بیش از ۱۰۰ باشد.");
    }
}