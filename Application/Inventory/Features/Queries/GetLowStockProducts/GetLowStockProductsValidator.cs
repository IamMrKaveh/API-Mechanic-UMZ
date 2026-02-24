namespace Application.Inventory.Features.Queries.GetLowStockProducts;

public class GetLowStockProductsValidator : AbstractValidator<GetLowStockProductsQuery>
{
    public GetLowStockProductsValidator()
    {
        RuleFor(x => x.Threshold)
            .GreaterThan(0)
            .WithMessage("آستانه موجودی کم باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(1000)
            .WithMessage("آستانه موجودی کم نمی‌تواند بیش از 1000 باشد.");
    }
}