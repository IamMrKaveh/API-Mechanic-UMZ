namespace Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره تلفن الزامی است.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("کد تأیید الزامی است.")
            .Length(6).WithMessage("کد تأیید باید ۶ رقم باشد.")
            .Matches(@"^\d{6}$").WithMessage("کد تأیید فقط باید شامل اعداد باشد.");

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("آدرس IP الزامی است.");
    }
}