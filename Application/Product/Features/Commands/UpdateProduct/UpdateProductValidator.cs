namespace Application.Product.Features.Commands.UpdateProduct;

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.UpdateProductInput.Id).GreaterThan(0);
        RuleFor(x => x.UpdateProductInput.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UpdateProductInput.RowVersion).NotEmpty();
        RuleFor(x => x.UpdateProductInput.Sku).MaximumLength(50).Matches("^[a-zA-Z0-9-_]*$");

        RuleForEach(x => x.UpdateProductInput.Images).ChildRules(images =>
        {
            images.RuleFor(i => i.FileSize).LessThan(5 * 1024 * 1024);
            images.RuleFor(i => i.FileType).Must(type => type.StartsWith("image/"));
        });
    }
}