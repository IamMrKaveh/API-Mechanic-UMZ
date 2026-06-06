namespace Application.Product.Features.Commands.ActivateProduct;

public sealed class ActivateProductValidator : AbstractValidator<ActivateProductCommand>
{
    public ActivateProductValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("شناسه محصول الزامی است.");
    }
}