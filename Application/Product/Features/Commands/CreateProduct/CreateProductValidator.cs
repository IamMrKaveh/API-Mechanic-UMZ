namespace Application.Product.Features.Commands.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.CategoryGroupId)
            .GreaterThan(0).WithMessage("Invalid Category Group.");

        RuleFor(x => x.Sku)
            .MaximumLength(50)
            .Matches(@"^[a-zA-Z0-9\-_]*$").WithMessage("SKU can only contain letters, numbers, dashes and underscores.");

        RuleFor(x => x.Variants)
            .NotEmpty().WithMessage("Product must have at least one variant.");

        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
            v.RuleFor(x => x.OriginalPrice).GreaterThanOrEqualTo(0);
            v.RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
            v.RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).When(x => !x.IsUnlimited);
        });

        RuleForEach(x => x.Images).ChildRules(images =>
        {
            images.RuleFor(i => i.Length).LessThan(5 * 1024 * 1024).WithMessage("File size must be less than 5MB.");
            images.RuleFor(i => i.ContentType).Must(type => type.StartsWith("image/")).WithMessage("Only image files are allowed.");
        });
    }
}