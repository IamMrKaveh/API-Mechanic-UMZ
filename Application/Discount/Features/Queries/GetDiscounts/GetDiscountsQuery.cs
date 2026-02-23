namespace Application.Discount.Features.Queries.GetDiscounts;

public record GetDiscountsQuery(
    bool IncludeExpired,
    bool IncludeDeleted,
    int Page,
    int PageSize
    ) : IRequest<ServiceResult<PaginatedResult<DiscountCodeDto>>>;