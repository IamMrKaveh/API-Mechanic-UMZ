using Application.Common.Validation;

namespace Application.Auth.Features.Commands.GoogleLogin;

public class GoogleLoginValidator : AbstractValidator<GoogleLoginCommand>
{

    public GoogleLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProviderKey).NotEmpty().MaximumLength(200);
    }
}
