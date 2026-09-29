using Application.Common.Validation;

namespace Application.Inventory.Features.Queries.GetInventoryStatus;

public class GetInventoryStatusValidator : AbstractValidator<GetInventoryStatusQuery>
{
    public GetInventoryStatusValidator()
    {
        this.RuleForRequiredId(x => x.VariantId, "شناسه واریانت الزامی است.");
    }
}