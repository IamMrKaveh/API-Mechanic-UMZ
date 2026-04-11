namespace Application.Product.Features.Queries.GetAdminProducts;

public sealed class GetAdminProductsValidator : AbstractValidator<GetAdminProductsQuery>
{
    public GetAdminProductsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("شماره صفحه باید بزرگ‌تر از صفر باشد.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("اندازه صفحه باید بین ۱ و ۱۰۰ باشد.");
    }
}