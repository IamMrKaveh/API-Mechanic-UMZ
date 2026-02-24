namespace Application.Shipping.Contracts;

public interface IShippingQueryService
{
    Task<IEnumerable<AvailableShippingDto>> GetAvailableShippingsForCartAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<ShippingCostResultDto> CalculateShippingCostAsync(
        int userId,
        int shippingId,
        CancellationToken ct = default
        );

    Task<IEnumerable<ShippingDto>> GetActiveShippingsAsync(
        CancellationToken ct = default
        );

    Task<ShippingDto?> GetShippingByIdAsync(
        int id,
        CancellationToken ct = default
        );

    Task<IEnumerable<AvailableShippingDto>> GetAvailableShippingsForVariantsAsync(
        IReadOnlyCollection<int> variantIds,
        CancellationToken ct = default
        );
}