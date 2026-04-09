using Application.Discount.Features.Shared;

namespace Application.Discount.Contracts;

public interface IDiscountService
{
    Task<ServiceResult<DiscountApplicationResult>> ApplyDiscountAsync(
        string code,
        decimal orderAmount,
        Guid userId,
        CancellationToken ct = default);
}