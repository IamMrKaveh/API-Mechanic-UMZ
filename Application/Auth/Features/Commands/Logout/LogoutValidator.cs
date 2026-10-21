using Application.Common.Validation;

namespace Application.Auth.Features.Commands.Logout;

public class LogoutValidator : AbstractValidator<LogoutCommand>
{

    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken).MaximumLength(512);
    }
}
