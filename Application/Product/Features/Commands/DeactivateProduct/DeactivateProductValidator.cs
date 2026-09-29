using Application.Common.Validation;

namespace Application.Product.Features.Commands.DeactivateProduct;

public sealed class DeactivateProductValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductValidator()
    {
        this.RuleForRequiredId(x => x.ProductId, "شناسه محصول الزامی است.");
    }
}