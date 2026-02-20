namespace Application.Variant.Features.Commands.RemoveVariant;

public class RemoveVariantValidator : AbstractValidator<RemoveVariantCommand>
{
    public RemoveVariantValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.VariantId).GreaterThan(0);
    }
}