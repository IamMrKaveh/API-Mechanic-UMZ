using Application.Common.Validation;

namespace Application.Variant.Features.Commands.RemoveVariant;

public class RemoveVariantValidator : AbstractValidator<RemoveVariantCommand>
{
    public RemoveVariantValidator()
    {
        this.RuleForRequiredId(x => x.ProductId);
        this.RuleForRequiredId(x => x.VariantId);
    }
}