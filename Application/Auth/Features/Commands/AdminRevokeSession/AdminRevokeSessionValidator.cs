using Application.Common.Validation;

namespace Application.Auth.Features.Commands.AdminRevokeSession;

public class AdminRevokeSessionValidator : AbstractValidator<AdminRevokeSessionCommand>
{

    public AdminRevokeSessionValidator()
    {
        this.RuleForRequiredId(x => x.TargetUserId);
        this.RuleForRequiredId(x => x.SessionId);
    }
}
