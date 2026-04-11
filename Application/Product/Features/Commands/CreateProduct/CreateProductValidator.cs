namespace Application.Product.Features.Commands.CreateProduct;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام محصول الزامی است.")
            .MaximumLength(200).WithMessage("نام محصول نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("توضیحات محصول الزامی است.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("دسته‌بندی الزامی است.");

        RuleFor(x => x.BrandId)
            .NotEmpty().WithMessage("برند الزامی است.");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("کاربر ایجادکننده الزامی است.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).WithMessage("Slug نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")
            .When(x => x.Slug is not null);
    }
}