using Application.Common.Validation;

namespace Application.User.Features.Commands.ChangePhoneNumber;

public class ChangePhoneNumberValidator : AbstractValidator<ChangePhoneNumberCommand>
{
    public ChangePhoneNumberValidator()
    {
        this.RuleForIranianPhoneNumber(x => x.NewPhoneNumber);

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("کد تأیید الزامی است.")
            .Length(6).WithMessage("کد تأیید باید ۶ رقم باشد.");
    }
}