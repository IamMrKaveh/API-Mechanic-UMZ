namespace Application.Variant.Features.Commands.UpdateProductVariantShipping;

public class UpdateVariantShippingValidator : AbstractValidator<UpdateVariantShippingCommand>
{
    public UpdateVariantShippingValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.ShippingMultiplier)
            .InclusiveBetween(0.1m, 100m)
            .WithMessage("ضریب هزینه ارسال باید بین 0.1 تا 100 باشد.");
        RuleFor(x => x.WeightGrams)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("وزن نمی‌تواند منفی باشد.")
            .LessThanOrEqualTo(500_000m)
            .WithMessage("وزن نمی‌تواند بیشتر از 500,000 گرم باشد.");
        RuleFor(x => x.EnabledShippingIds).NotNull();
    }
}