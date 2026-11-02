using Application.Common.Validation;

namespace Application.User.Features.Commands.RequestPhoneChangeOtp;

public class RequestPhoneChangeOtpValidator : AbstractValidator<RequestPhoneChangeOtpCommand>
{
    public RequestPhoneChangeOtpValidator()
    {
        this.RuleForIranianPhoneNumber(x => x.NewPhoneNumber);
    }
}
