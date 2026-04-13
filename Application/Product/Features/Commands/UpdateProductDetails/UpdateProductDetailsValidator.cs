namespace Application.Product.Features.Commands.UpdateProductDetails;

public sealed class UpdateProductDetailsValidator : AbstractValidator<UpdateProductDetailsCommand>
{
    public UpdateProductDetailsValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("شناسه محصول الزامی است.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام محصول الزامی است.")
            .MaximumLength(200).WithMessage("نام محصول نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");

        RuleFor(x => x.BrandId)
            .NotEmpty().WithMessage("برند الزامی است.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion الزامی است.");

        RuleFor(x => x.Sku)
            .MaximumLength(50).WithMessage("SKU نمی‌تواند بیش از ۵۰ کاراکتر باشد.")
            .Matches(@"^[a-zA-Z0-9\-_]*$").WithMessage("SKU فقط می‌تواند شامل حروف، اعداد، خط تیره و زیرخط باشد.")
            .When(x => !string.IsNullOrEmpty(x.Sku));
    }
}