using Application.Discount.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Discount.Contracts;

public interface IDiscountService
{
    Task<ServiceResult<DiscountApplicationResult>> ApplyDiscountAsync(
        string code,
        Money orderAmount,
        UserId userId,
        CancellationToken ct = default);
}