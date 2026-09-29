using Application.Common.Validation;

namespace Application.Wallet.Features.Commands.DismissFraudAlert;

public sealed class DismissFraudAlertValidator : AbstractValidator<DismissFraudAlertCommand>
{
    public DismissFraudAlertValidator()
    {
        this.RuleForRequiredId(x => x.AlertId, "شناسه هشدار الزامی است.");
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("طول توضیحات نباید بیش از ۵۰۰ کاراکتر باشد.");
    }
}