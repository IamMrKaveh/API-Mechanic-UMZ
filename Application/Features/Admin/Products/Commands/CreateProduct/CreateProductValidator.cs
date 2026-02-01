namespace Application.Features.Admin.Products.Commands.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("نام محصول الزامی است.").MaximumLength(200).WithMessage("نام محصول نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");

        RuleFor(x => x.CategoryGroupId)
            .GreaterThan(0).WithMessage("گروه دسته‌بندی نامعتبر است.");

        RuleFor(x => x.Sku)
            .MaximumLength(50).WithMessage("کد SKU نمی‌تواند بیش از ۵۰ کاراکتر باشد.")
            .Matches("^[a-zA-Z0-9-_]*$").WithMessage("کد SKU فقط می‌تواند شامل حروف، اعداد و خط تیره باشد.");

        RuleForEach(x => x.Images).ChildRules(images =>
        {
            images.RuleFor(i => i.Length).LessThan(5 * 1024 * 1024).WithMessage("حجم فایل نباید بیش از ۵ مگابایت باشد.");
            images.RuleFor(i => i.ContentType).Must(type => type.StartsWith("image/")).WithMessage("فقط فایل‌های تصویری مجاز هستند.");
        });
    }
}