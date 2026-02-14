namespace Application.Cart.Features.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityValidator()
    {
        RuleFor(x => x.VariantId)
            .GreaterThan(0).WithMessage("شناسه واریانت معتبر نیست.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("تعداد نمی‌تواند منفی باشد.")
            .LessThanOrEqualTo(1000).WithMessage("حداکثر تعداد مجاز ۱۰۰۰ عدد است.");
    }
}