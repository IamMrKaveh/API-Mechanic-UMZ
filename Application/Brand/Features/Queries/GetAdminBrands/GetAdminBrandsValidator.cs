namespace Application.Brand.Features.Queries.GetAdminBrands;

public class GetAdminBrandsValidator : AbstractValidator<GetAdminBrandsQuery>
{
    public GetAdminBrandsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}