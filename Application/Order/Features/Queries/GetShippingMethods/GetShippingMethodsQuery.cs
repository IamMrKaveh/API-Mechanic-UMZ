namespace Application.Order.Features.Queries.GetShippingMethods;

public record GetShippingMethodsQuery(bool IncludeDeleted = false) : IRequest<ServiceResult<IEnumerable<ShippingMethodDto>>>;