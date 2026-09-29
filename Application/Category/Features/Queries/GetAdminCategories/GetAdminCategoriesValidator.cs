using Application.Common.Validation;

namespace Application.Category.Features.Queries.GetAdminCategories;

public class GetAdminCategoriesValidator : AbstractValidator<GetAdminCategoriesQuery>
{
    public GetAdminCategoriesValidator()
    {
        this.RuleForPagination(x => x.Page, x => x.PageSize);
    }
}