using Application.Common.Validation;

namespace Application.Inventory.Features.Queries.GetInventoryTransactions;

public class GetInventoryTransactionsValidator : AbstractValidator<GetInventoryTransactionsQuery>
{
    public GetInventoryTransactionsValidator()
    {
        this.RuleForPagination(x => x.Page, x => x.PageSize);

        this.RuleForOptionalDateRange(
            x => x.FromDate,
            x => x.ToDate,
            ruleOnFromProperty: false,
            message: "تاریخ پایان باید بزرگتر یا مساوی تاریخ شروع باشد.");
    }
}