using Application.Common.Validation;

namespace Application.Attribute.Features.Commands.CreateAttributeValue;

public class CreateAttributeValueValidator : AbstractValidator<CreateAttributeValueCommand>
{

    public CreateAttributeValueValidator()
    {
        this.RuleForRequiredId(x => x.TypeId);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayValue).NotEmpty().MaximumLength(200);
        RuleFor(x => x.HexCode).MaximumLength(7);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
