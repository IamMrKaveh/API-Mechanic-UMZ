using Application.Common.Validation;

namespace Application.Attribute.Features.Commands.CreateAttributeType;

public class CreateAttributeTypeValidator : AbstractValidator<CreateAttributeTypeCommand>
{

    public CreateAttributeTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
