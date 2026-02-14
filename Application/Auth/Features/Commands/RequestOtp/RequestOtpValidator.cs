namespace Application.Auth.Features.Commands.RequestOtp;

public class RequestOtpValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره تلفن الزامی است.")
            .Matches(@"^(\+98|0098|98|0)?9\d{9}$").WithMessage("فرمت شماره تلفن نامعتبر است.");

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("آدرس IP الزامی است.");
    }
}