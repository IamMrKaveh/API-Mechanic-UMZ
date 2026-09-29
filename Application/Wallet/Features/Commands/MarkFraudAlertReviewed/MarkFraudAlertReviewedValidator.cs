using Application.Common.Validation;

namespace Application.Wallet.Features.Commands.MarkFraudAlertReviewed;

public sealed class MarkFraudAlertReviewedValidator : AbstractValidator<MarkFraudAlertReviewedCommand>
{
    public MarkFraudAlertReviewedValidator()
    {
        this.RuleForRequiredId(x => x.AlertId, "شناسه هشدار الزامی است.");
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("طول یادداشت نباید بیش از ۵۰۰ کاراکتر باشد.");
    }
}