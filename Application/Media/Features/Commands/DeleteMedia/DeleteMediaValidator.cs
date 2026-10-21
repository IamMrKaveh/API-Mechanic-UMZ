using Application.Common.Validation;

namespace Application.Media.Features.Commands.DeleteMedia;

public class DeleteMediaValidator : AbstractValidator<DeleteMediaCommand>
{

    public DeleteMediaValidator()
    {
        this.RuleForRequiredId(x => x.MediaId);
    }
}
