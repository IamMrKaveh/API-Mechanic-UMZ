namespace Application.Payment.Features.Commands.CreatePaymentMethod;

public sealed class CreatePaymentMethodValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    public CreatePaymentMethodValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FeeAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FeePercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.IconUrl).MaximumLength(500);
    }
}