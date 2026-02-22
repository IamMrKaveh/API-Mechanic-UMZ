namespace Application.Shipping.Features.Queries.GetShippings;

public record GetShippingsQuery(
    bool IncludeDeleted = false
    ) : IRequest<ServiceResult<IEnumerable<ShippingDto>>>;