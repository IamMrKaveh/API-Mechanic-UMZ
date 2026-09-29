using Application.Common.Validation;

namespace Application.Product.Features.Commands.ActivateProduct;

public sealed class ActivateProductValidator : AbstractValidator<ActivateProductCommand>
{
    public ActivateProductValidator()
    {
        this.RuleForRequiredId(x => x.ProductId, "شناسه محصول الزامی است.");
    }
}