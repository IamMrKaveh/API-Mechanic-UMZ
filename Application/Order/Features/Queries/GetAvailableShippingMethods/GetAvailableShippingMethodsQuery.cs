namespace Application.Order.Features.Queries.GetAvailableShippingMethods;

public record GetAvailableShippingMethodsQuery(int UserId)
    : IRequest<ServiceResult<IEnumerable<AvailableShippingMethodDto>>>;