using Application.Common.Validation;

namespace Application.Attribute.Features.Commands.DeleteAttributeValue;

public class DeleteAttributeValueValidator : AbstractValidator<DeleteAttributeValueCommand>
{

    public DeleteAttributeValueValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
