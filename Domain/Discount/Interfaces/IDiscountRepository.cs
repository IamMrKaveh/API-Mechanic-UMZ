using Domain.Common.Interfaces;
using Domain.Discount.Aggregates;
using Domain.Discount.ValueObjects;

namespace Domain.Discount.Interfaces;

public interface IDiscountRepository : IRepository<DiscountCode, DiscountCodeId>
{
    Task<DiscountCode?> GetByCodeAsync(
        string code,
        CancellationToken ct = default);

    Task<DiscountCode?> GetByIdWithUsagesAsync(
        DiscountCodeId id,
        CancellationToken ct = default);
}