using Application.Common.Validation;

namespace Application.Product.Features.Queries.GetAdminProducts;

public sealed class GetAdminProductsValidator : AbstractValidator<GetAdminProductsQuery>
{
    public GetAdminProductsValidator()
    {
        this.RuleForPagination(x => x.Page, x => x.PageSize);
    }
}