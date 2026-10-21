using Application.Common.Validation;

namespace Application.Auth.Features.Commands.RevokeSession;

public class RevokeSessionValidator : AbstractValidator<RevokeSessionCommand>
{

    public RevokeSessionValidator()
    {
        this.RuleForRequiredId(x => x.SessionId);
    }
}
