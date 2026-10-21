using Application.Common.Validation;

namespace Application.Media.Features.Commands.SetPrimaryMedia;

public class SetPrimaryMediaValidator : AbstractValidator<SetPrimaryMediaCommand>
{

    public SetPrimaryMediaValidator()
    {
        this.RuleForRequiredId(x => x.MediaId);
    }
}
