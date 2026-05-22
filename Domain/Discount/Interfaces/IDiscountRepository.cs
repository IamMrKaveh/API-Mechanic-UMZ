using Domain.Discount.Aggregates;
using Domain.Discount.ValueObjects;

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

    Task AddAsync(
        DiscountCode discount,
        CancellationToken ct = default);

    void Update(DiscountCode discount);
}