namespace Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("توکن الزامی است.");

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("آدرس IP الزامی است.");
    }
}