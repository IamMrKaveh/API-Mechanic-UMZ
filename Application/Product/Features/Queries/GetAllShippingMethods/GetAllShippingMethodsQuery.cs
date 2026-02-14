namespace Application.Product.Features.Queries.GetAllShippingMethods;

public record GetAllShippingMethodsQuery : IRequest<ServiceResult<IEnumerable<ShippingMethodDto>>>;