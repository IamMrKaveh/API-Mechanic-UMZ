using Application.Common.Validation;

namespace Application.User.Features.Commands.ChangeUserRole;

public class ChangeUserRoleValidator : AbstractValidator<ChangeUserRoleCommand>
{

    public ChangeUserRoleValidator()
    {
        this.RuleForRequiredId(x => x.UserId);
    }
}
