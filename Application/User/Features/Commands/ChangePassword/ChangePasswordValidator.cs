namespace Application.User.Features.Commands.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.Dto.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.Dto.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.");

        RuleFor(x => x.Dto.ConfirmNewPassword)
            .Equal(x => x.Dto.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}