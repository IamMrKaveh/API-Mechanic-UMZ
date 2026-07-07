namespace Application.Wallet.Features.Commands.DismissFraudAlert;

public sealed class DismissFraudAlertValidator : AbstractValidator<DismissFraudAlertCommand>
{
    public DismissFraudAlertValidator()
    {
        RuleFor(x => x.AlertId).NotEmpty().WithMessage("شناسه هشدار الزامی است.");
        RuleFor(x => x.AdminId).NotEmpty().WithMessage("شناسه ادمین الزامی است.");
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("طول توضیحات نباید بیش از ۵۰۰ کاراکتر باشد.");
    }
}