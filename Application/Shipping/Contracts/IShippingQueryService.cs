using Application.Shipping.Features.Shared;

namespace Application.Shipping.Contracts;

public interface IShippingQueryService
{
    Task<ShippingDto?> GetShippingDetailAsync(int shippingId, CancellationToken ct = default);

    Task<IReadOnlyList<ShippingListItemDto>> GetAllShippingsAsync(
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<IReadOnlyList<ShippingListItemDto>> GetAvailableShippingsForOrderAsync(
        decimal orderAmount,
        CancellationToken ct = default);
}