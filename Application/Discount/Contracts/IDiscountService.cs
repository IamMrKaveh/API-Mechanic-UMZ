using Application.Common.Results;
using Application.Discount.Features.Shared;

namespace Application.Discount.Contracts;

public interface IDiscountService
{
    Task<ServiceResult<DiscountApplicationResultDto>> ApplyDiscountAsync(
        string code,
        decimal orderAmount,
        Guid userId,
        CancellationToken ct = default);
}