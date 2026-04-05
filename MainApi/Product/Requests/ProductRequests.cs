using Application.Product.Features.Shared;

namespace MainApi.Product.Requests;

public sealed record ChangePriceRequest(decimal NewPrice);

public sealed record BulkUpdatePricesRequest(IReadOnlyList<ProductPriceUpdateItem> Items);

public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int CategoryId,
    int BrandId);