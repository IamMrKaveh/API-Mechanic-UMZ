using Application.Common.Models;

namespace Application.Order.Features.Queries.GetOrderDetails;

public record GetOrderDetailsQuery(int OrderId, int UserId) : IRequest<ServiceResult<OrderDto>>;