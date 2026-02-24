namespace Application.Order.Features.Queries.GetOrderDetails;

public record GetOrderDetailsQuery(int OrderId, int UserId) : IRequest<ServiceResult<OrderDto>>;