namespace Application.Category.Features.Queries.GetCategoryProducts;

public class GetCategoryProductsValidator : AbstractValidator<GetCategoryProductsQuery>
{
    public GetCategoryProductsValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}