namespace Application.Inventory.Features.Queries.GetInventoryStatus;

public class GetInventoryStatusValidator : AbstractValidator<GetInventoryStatusQuery>
{
    public GetInventoryStatusValidator()
    {
        RuleFor(x => x.VariantId)
            .GreaterThan(0)
            .WithMessage("شناسه واریانت باید بزرگتر از صفر باشد.");
    }
}