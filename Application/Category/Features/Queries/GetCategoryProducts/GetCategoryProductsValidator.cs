using Application.Common.Validation;

namespace Application.Category.Features.Queries.GetCategoryProducts;

public class GetCategoryProductsValidator : AbstractValidator<GetCategoryProductsQuery>
{
    public GetCategoryProductsValidator()
    {
        this.RuleForRequiredId(x => x.CategoryId, "شناسه دسته‌بندی الزامی است.");
        this.RuleForPagination(x => x.Page, x => x.PageSize);
    }
}