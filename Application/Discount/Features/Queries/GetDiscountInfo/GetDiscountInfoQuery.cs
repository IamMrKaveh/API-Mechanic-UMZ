using Application.Common.Models;

namespace Application.Discount.Features.Queries.GetDiscountInfo;

public record GetDiscountInfoQuery(string Code) : IRequest<ServiceResult<DiscountInfoDto>>;