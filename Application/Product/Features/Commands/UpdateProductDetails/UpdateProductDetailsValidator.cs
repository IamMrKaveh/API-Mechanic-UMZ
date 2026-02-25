namespace Application.Product.Features.Commands.UpdateProductDetails;

public class UpdateProductDetailsValidator : AbstractValidator<UpdateProductDetailsCommand>
{
    public UpdateProductDetailsValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BrandId).GreaterThan(0);
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Sku).MaximumLength(50).Matches(@"^[a-zA-Z0-9\-_]*$");
    }
}