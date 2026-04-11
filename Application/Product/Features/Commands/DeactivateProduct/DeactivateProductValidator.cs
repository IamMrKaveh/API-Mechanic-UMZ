namespace Application.Product.Features.Commands.DeactivateProduct;

public sealed class DeactivateProductValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("شناسه محصول الزامی است.");

        RuleFor(x => x.DeactivatedByUserId)
            .NotEmpty().WithMessage("کاربر غیرفعال‌کننده الزامی است.");
    }
}