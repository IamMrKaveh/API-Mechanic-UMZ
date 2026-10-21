using Application.Common.Validation;

namespace Application.User.Features.Commands.DeleteUser;

public class DeleteUserValidator : AbstractValidator<DeleteUserCommand>
{

    public DeleteUserValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
