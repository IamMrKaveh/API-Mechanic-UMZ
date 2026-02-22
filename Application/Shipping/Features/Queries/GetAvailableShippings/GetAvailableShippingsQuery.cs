namespace Application.Shipping.Features.Queries.GetAvailableShippings;

public record GetAvailableShippingsQuery(int UserId)
    : IRequest<ServiceResult<IEnumerable<AvailableShippingDto>>>;