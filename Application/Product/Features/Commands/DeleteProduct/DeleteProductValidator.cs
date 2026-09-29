using Application.Common.Validation;

namespace Application.Product.Features.Commands.DeleteProduct;

public sealed class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductValidator()
    {
        this.RuleForRequiredId(x => x.ProductId, "شناسه محصول الزامی است.");
    }
}