namespace Application.Features.Admin.Products.Commands.UpdateProduct;

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.RowVersion).NotEmpty(); RuleFor(x => x.Sku).MaximumLength(50).Matches("^[a-zA-Z0-9-_]*$");

        RuleForEach(x => x.Images).ChildRules(images =>
        {
            images.RuleFor(i => i.Length).LessThan(5 * 1024 * 1024);
            images.RuleFor(i => i.ContentType).Must(type => type.StartsWith("image/"));
        });
    }
}