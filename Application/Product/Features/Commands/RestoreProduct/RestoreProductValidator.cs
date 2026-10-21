using Application.Common.Validation;

namespace Application.Product.Features.Commands.RestoreProduct;

public class RestoreProductValidator : AbstractValidator<RestoreProductCommand>
{

    public RestoreProductValidator()
    {
        this.RuleForRequiredId(x => x.ProductId);
    }
}
