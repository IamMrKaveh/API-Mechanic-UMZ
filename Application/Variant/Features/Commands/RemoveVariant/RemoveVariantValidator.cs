namespace Application.Variant.Features.Commands.RemoveVariant;

public class RemoveVariantValidator : AbstractValidator<RemoveVariantCommand>
{
    public RemoveVariantValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VariantId).NotEmpty();
    }
}