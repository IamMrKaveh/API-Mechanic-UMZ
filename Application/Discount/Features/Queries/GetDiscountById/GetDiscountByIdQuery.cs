using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.GetDiscountById;

public record GetDiscountByIdQuery(Guid Id) : IRequest<ServiceResult<DiscountCodeDetailDto?>>;