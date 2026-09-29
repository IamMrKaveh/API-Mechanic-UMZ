using Application.Common.Validation;

namespace Application.Cart.Features.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityValidator()
    {
        this.RuleForRequiredId(x => x.VariantId, "شناسه واریانت معتبر نیست.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("تعداد نمی‌تواند منفی باشد.")
            .LessThanOrEqualTo(1000).WithMessage("حداکثر تعداد مجاز ۱۰۰۰ عدد است.");
    }
}