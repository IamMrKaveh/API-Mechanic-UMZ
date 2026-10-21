using Application.Common.Validation;

namespace Application.User.Features.Commands.RestoreUser;

public class RestoreUserValidator : AbstractValidator<RestoreUserCommand>
{

    public RestoreUserValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
