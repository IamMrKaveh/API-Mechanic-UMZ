using Application.Common.Validation;

namespace Application.Attribute.Features.Commands.DeleteAttributeType;

public class DeleteAttributeTypeValidator : AbstractValidator<DeleteAttributeTypeCommand>
{

    public DeleteAttributeTypeValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
