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