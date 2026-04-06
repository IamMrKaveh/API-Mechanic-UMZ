namespace Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^09\d{9}$").WithMessage("فرمت شماره موبایل نامعتبر است.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("کد OTP الزامی است.")
            .Length(4, 8).WithMessage("کد OTP باید بین ۴ تا ۸ رقم باشد.")
            .Matches(@"^\d+$").WithMessage("کد OTP فقط باید شامل اعداد باشد.");
    }
}