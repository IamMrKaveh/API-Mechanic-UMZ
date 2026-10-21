using Application.Common.Validation;

namespace Application.User.Features.Commands.CreateUser;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{

    public CreateUserValidator()
    {
        this.RuleForIranianMobileNumber(x => x.PhoneNumber);
        RuleFor(x => x.FirstName).MaximumLength(50);
        RuleFor(x => x.LastName).MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress();
    }
}
