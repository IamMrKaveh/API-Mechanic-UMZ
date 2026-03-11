namespace Application.Variant.Features.Shared;

public record ProductVariantDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal FinalPrice { get; init; }
    public int StockQuantity { get; init; }
}

public record VariantStockChangedApplicationNotification : INotification
{
    public int VariantId { get; init; }
    public int ProductId { get; init; }
    public int QuantityChanged { get; init; }
    public int NewOnHand { get; init; }
    public int NewReserved { get; init; }
    public int NewAvailable { get; init; }
    public bool IsInStock { get; init; }
}