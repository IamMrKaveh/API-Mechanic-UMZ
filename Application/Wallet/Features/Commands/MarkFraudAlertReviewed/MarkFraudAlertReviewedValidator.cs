namespace Application.Wallet.Features.Commands.MarkFraudAlertReviewed;

public sealed class MarkFraudAlertReviewedValidator : AbstractValidator<MarkFraudAlertReviewedCommand>
{
    public MarkFraudAlertReviewedValidator()
    {
        RuleFor(x => x.AlertId).NotEmpty().WithMessage("شناسه هشدار الزامی است.");
        RuleFor(x => x.AdminId).NotEmpty().WithMessage("شناسه ادمین الزامی است.");
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("طول یادداشت نباید بیش از ۵۰۰ کاراکتر باشد.");
    }
}