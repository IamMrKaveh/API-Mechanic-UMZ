using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.GetDiscounts;

public class GetDiscountsHandler(
    IDiscountQueryService discountQueryService) : IRequestHandler<GetDiscountsQuery, ServiceResult<PaginatedResult<DiscountCodeDto>>>
{
    public async Task<ServiceResult<PaginatedResult<DiscountCodeDto>>> Handle(
        GetDiscountsQuery request, CancellationToken ct)
    {
        var (discounts, total) = await discountQueryService.GetPagedAsync(
            request.IncludeExpired,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<DiscountCodeDto>>.Success(
            PaginatedResult<DiscountCodeDto>.Create(
                discounts.ToList(),
                total,
                request.Page,
                request.PageSize));
    }
}