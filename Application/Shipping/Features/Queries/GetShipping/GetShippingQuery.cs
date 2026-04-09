using Application.Common.Results;
using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShipping;

public record GetShippingQuery(Guid Id) : IRequest<ServiceResult<ShippingDto>>;