using Application.Common.Results;
using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShippingById;

public record GetShippingByIdQuery(int Id) : IRequest<ServiceResult<ShippingDto>>;