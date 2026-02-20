using Application.Shipping.Features.Shared;

namespace Application.Product.Features.Queries.GetAllShipping;

public record GetAllShippingQuery : IRequest<ServiceResult<IEnumerable<ShippingMethodDto>>>;