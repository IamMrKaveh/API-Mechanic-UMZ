using Application.Common.Validation;

namespace Application.Cart.Features.Commands.AddItemToCart;

public class AddItemToCartValidator : AbstractValidator<AddItemToCartCommand>
{
    public AddItemToCartValidator()
    {
        this.RuleForRequiredId(x => x.VariantId, "شناسه محصول الزامی است.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("تعداد باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(100).WithMessage("تعداد نمی‌تواند بیش از ۱۰۰ باشد.");
    }
}