using Application.Common.Validation;

namespace Application.User.Features.Commands.UpdateUser;

public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{

    public UpdateUserValidator()
    {
        this.RuleForRequiredId(x => x.Id);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
    }
}
