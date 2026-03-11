namespace Domain.Discount.Interfaces;

public interface IDiscountRepository
{
    Task<DiscountCode?> GetByIdAsync(
        DiscountCodeId id,
        CancellationToken ct = default);

    Task<DiscountCode?> GetByCodeAsync(
        string code,
        CancellationToken ct = default);

    Task<DiscountCode?> GetByIdWithUsagesAsync(
        DiscountCodeId id,
        CancellationToken ct = default);

    Task<bool> ExistsByCodeAsync(
        string code,
        DiscountCodeId? excludeId = null,
        CancellationToken ct = default);

    Task<int> CountUserUsageAsync(
        DiscountCodeId discountId,
        Guid userId,
        CancellationToken ct = default);

    Task AddAsync(
        DiscountCode discount,
        CancellationToken ct = default);

    void Update(DiscountCode discount);

    void SetOriginalRowVersion(
        DiscountCode entity,
        byte[] rowVersion);
}