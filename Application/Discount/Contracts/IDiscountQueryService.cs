using Application.Discount.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Discount.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Discount.Contracts;

public interface IDiscountQueryService
{
    Task<DiscountDto?> GetByIdAsync(
        DiscountCodeId discountCodeId,
        CancellationToken ct = default);

    Task<DiscountDto?> GetByCodeAsync(
        string code,
        CancellationToken ct = default);

    Task<DiscountValidationResult> ValidateDiscountAsync(
        string code,
        Money orderAmount,
        UserId userId,
        CancellationToken ct = default);

    Task<PaginatedResult<DiscountDto>> GetDiscountsPagedAsync(
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default);
}