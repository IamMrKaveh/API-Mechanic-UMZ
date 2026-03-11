using Application.Common.Models;

namespace Application.Shipping.Features.Queries.GetShippingById;

public record GetShippingByIdQuery(int Id) : IRequest<ServiceResult<ShippingDto>>;