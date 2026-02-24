namespace Application.Cart.Features.Commands.SyncCartPrices;

public record SyncCartPricesResult
{
    public bool HasChanges { get; init; }
    public List<CartPriceChangeDto> PriceChanges { get; init; } = new();
    public List<int> RemovedVariantIds { get; init; } = new();
}