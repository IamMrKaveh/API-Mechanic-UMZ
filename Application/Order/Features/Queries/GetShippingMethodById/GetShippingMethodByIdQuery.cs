namespace Application.Order.Features.Queries.GetShippingMethodById;

public record GetShippingMethodByIdQuery(int Id) : IRequest<ServiceResult<ShippingMethodDto>>;