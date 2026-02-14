namespace Application.Product.Features.Commands.ChangePrice;

public record ChangePriceCommand : IRequest<ServiceResult>
{
    public int ProductId { get; init; }
    public int VariantId { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal OriginalPrice { get; init; }
}