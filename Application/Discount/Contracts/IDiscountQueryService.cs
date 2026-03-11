namespace Application.Discount.Contracts;

public interface IDiscountQueryService
{
    Task<(IEnumerable<DiscountCodeDto> Discounts, int TotalCount)> GetPagedAsync(
        bool includeExpired,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<DiscountCodeDetailDto?> GetDetailByIdAsync(
        int id,
        CancellationToken ct = default);

    Task<IEnumerable<DiscountCodeDto>> GetActiveDiscountsAsync(
        CancellationToken ct = default);

    Task<IEnumerable<DiscountCodeDto>> GetExpiringDiscountsAsync(
        DateTime beforeDate,
        CancellationToken ct = default);

    Task<DiscountInfoDto?> GetDiscountInfoByCodeAsync(
        string code,
        CancellationToken ct = default);

    Task<DiscountUsageReportDto?> GetUsageReportByIdAsync(
        int id,
        CancellationToken ct = default);
}