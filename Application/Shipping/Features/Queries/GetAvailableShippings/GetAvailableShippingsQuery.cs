namespace Application.Shipping.Features.Queries.GetAvailableShippings;

public record GetAvailableShippingsQuery(Guid UserId) : IRequest<ServiceResult<IEnumerable<AvailableShippingDto>>>;