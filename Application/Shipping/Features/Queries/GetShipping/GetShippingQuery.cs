using Application.Common.Results;
using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShipping;

public record GetShippingQuery(int Id) : IRequest<ServiceResult<ShippingDto>>;