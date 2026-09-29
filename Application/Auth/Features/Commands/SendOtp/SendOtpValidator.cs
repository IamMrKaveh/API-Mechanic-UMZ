using Application.Common.Validation;

namespace Application.Auth.Features.Commands.SendOtp;

public class SendOtpValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpValidator()
    {
        this.RuleForIranianMobileNumber(x => x.PhoneNumber);
    }
}