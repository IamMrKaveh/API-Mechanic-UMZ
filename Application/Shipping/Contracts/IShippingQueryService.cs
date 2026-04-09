using Application.Shipping.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Shipping.ValueObjects;

namespace Application.Shipping.Contracts;

public interface IShippingQueryService
{
    Task<ShippingDto?> GetShippingDetailAsync(
        ShippingId shippingId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ShippingListItemDto>> GetAllShippingsAsync(
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<IReadOnlyList<ShippingListItemDto>> GetAvailableShippingsForOrderAsync(
        Money orderAmount,
        CancellationToken ct = default);
}