namespace Application.Auth.Features.Commands.SendOtp;

public class SendOtpValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .Matches(@"^09\d{9}$").WithMessage("فرمت شماره موبایل نامعتبر است.");
    }
}