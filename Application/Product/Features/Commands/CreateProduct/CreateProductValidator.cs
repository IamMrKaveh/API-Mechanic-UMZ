namespace Application.Product.Features.Commands.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Input.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Input.CategoryId)
            .GreaterThan(0).WithMessage("Invalid Category.");

        RuleFor(x => x.Input.Slug)
            .NotEmpty().WithMessage("Slug is required.");
    }
}