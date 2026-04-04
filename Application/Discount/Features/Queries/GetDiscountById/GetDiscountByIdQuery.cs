using Application.Common.Results;
using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.GetDiscountById;

public record GetDiscountByIdQuery(int Id) : IRequest<ServiceResult<DiscountCodeDetailDto?>>;