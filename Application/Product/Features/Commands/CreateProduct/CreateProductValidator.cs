namespace Application.Product.Features.Commands.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام محصول الزامی است.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("توضیحات محصول الزامی است.");

        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("دسته‌بندی الزامی است.");
        RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("برند الزامی است.");
    }
}