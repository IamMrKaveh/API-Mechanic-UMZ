namespace Application.Category.Features.Queries.GetAdminCategories;

public class GetAdminCategoriesValidator : AbstractValidator<GetAdminCategoriesQuery>
{
    public GetAdminCategoriesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}