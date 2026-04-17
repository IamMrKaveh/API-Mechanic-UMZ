using Application.Discount.Features.Shared;
using Domain.Discount.ValueObjects;

namespace Application.Discount.Contracts;

public interface IDiscountQueryService
{
    Task<DiscountCodeDetailDto?> GetDetailByIdAsync(
        DiscountCodeId id,
        CancellationToken ct = default);

    Task<DiscountInfoDto?> GetDiscountInfoByCodeAsync(
        string code,
        CancellationToken ct = default);

    Task<DiscountValidationResult> ValidateDiscountAsync(
        string code,
        Money orderAmount,
        Guid userId,
        CancellationToken ct = default);

    Task<(IReadOnlyCollection<DiscountCodeDto> Items, int Total)> GetPagedAsync(
        bool includeExpired,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<DiscountUsageReportDto?> GetUsageReportByIdAsync(
        DiscountCodeId discountCodeId,
        CancellationToken ct = default);
}