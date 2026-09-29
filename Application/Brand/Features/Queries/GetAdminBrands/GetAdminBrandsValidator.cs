using Application.Common.Validation;

namespace Application.Brand.Features.Queries.GetAdminBrands;

public class GetAdminBrandsValidator : AbstractValidator<GetAdminBrandsQuery>
{
    public GetAdminBrandsValidator()
    {
        this.RuleForPagination(x => x.Page, x => x.PageSize);
    }
}