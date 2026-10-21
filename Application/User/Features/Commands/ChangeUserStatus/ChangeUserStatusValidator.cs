using Application.Common.Validation;

namespace Application.User.Features.Commands.ChangeUserStatus;

public class ChangeUserStatusValidator : AbstractValidator<ChangeUserStatusCommand>
{

    public ChangeUserStatusValidator()
    {
        this.RuleForRequiredId(x => x.UserId);
    }
}
