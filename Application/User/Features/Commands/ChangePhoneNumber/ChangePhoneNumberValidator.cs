namespace Application.User.Features.Commands.ChangePhoneNumber;

public class ChangePhoneNumberValidator : AbstractValidator<ChangePhoneNumberCommand>
{
    public ChangePhoneNumberValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);

        RuleFor(x => x.NewPhoneNumber)
            .NotEmpty().WithMessage("شماره تلفن جدید الزامی است.")
            .Matches(@"^(\+98|0098|98|0)?9\d{9}$").WithMessage("فرمت شماره تلفن نامعتبر است.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("کد تأیید الزامی است.")
            .Length(6).WithMessage("کد تأیید باید ۶ رقم باشد.");
    }
}