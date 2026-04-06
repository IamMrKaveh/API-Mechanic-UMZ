using Application.Discount.Features.Shared;
using SharedKernel.Models;

namespace Application.Discount.Contracts;

public interface IDiscountQueryService
{
    Task<DiscountDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DiscountDto?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<DiscountValidationResultDto> ValidateDiscountAsync(string code, decimal orderAmount, Guid userId, CancellationToken ct = default);

    Task<PaginatedResult<DiscountDto>> GetDiscountsPagedAsync(
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default);
}