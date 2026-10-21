using Application.Common.Validation;

namespace Application.Auth.Features.Commands.LogoutAll;

public class LogoutAllValidator : AbstractValidator<LogoutAllCommand>
{
    public LogoutAllValidator()
    {
        this.RuleFor(x => x.TargetUserId)
            .NotEqual(Guid.Empty)
            .WithMessage("شناسه کاربر نامعتبر است.");
    }
}
