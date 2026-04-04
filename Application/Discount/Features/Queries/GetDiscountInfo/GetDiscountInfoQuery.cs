using Application.Common.Results;
using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.GetDiscountInfo;

public record GetDiscountInfoQuery(string Code) : IRequest<ServiceResult<DiscountInfoDto>>;