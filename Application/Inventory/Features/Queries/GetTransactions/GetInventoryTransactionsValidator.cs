namespace Application.Inventory.Features.Queries.GetTransactions;

public class GetInventoryTransactionsValidator : AbstractValidator<GetInventoryTransactionsQuery>
{
    public GetInventoryTransactionsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("شماره صفحه باید بزرگتر از صفر باشد.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("اندازه صفحه باید بین 1 تا 100 باشد.");

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("تاریخ پایان باید بزرگتر یا مساوی تاریخ شروع باشد.");
        });
    }
}