namespace Application.User.Features.Commands.UpdateProfile;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).MaximumLength(50).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(50).When(x => x.LastName is not null);
    }
}