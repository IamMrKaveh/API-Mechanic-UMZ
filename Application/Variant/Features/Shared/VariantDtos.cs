namespace Application.Variant.Features.Shared;

public record ProductVariantDto(
    int Id,
    int ProductId,
    string Sku,
    decimal Price,
    decimal FinalPrice,
    int StockQuantity
);

public record CreateVariantInput(
    int ProductId,
    string Sku,
    decimal Price,
    int StockQuantity,
    decimal? DiscountAmount,
    bool IsDiscountPercentage
);

/// <summary>
/// Application-level notification که داده‌های enriched را برای Cache و Search حمل می‌کند
/// این کلاس جایگزین payload اضافی در VariantStockChangedEvent می‌شود
/// Handler این notification مسئول fetch داده از DB و به‌روزرسانی Cache/Search است
/// </summary>
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