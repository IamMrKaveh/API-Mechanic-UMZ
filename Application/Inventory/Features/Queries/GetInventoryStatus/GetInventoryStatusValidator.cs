namespace Application.Inventory.Features.Queries.GetInventoryStatus;

public class GetInventoryStatusValidator : AbstractValidator<GetInventoryStatusQuery>
{
    public GetInventoryStatusValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty().WithMessage("شناسه واریانت الزامی است.");
    }
}