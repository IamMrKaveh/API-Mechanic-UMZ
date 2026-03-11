using Application.Common.Models;

namespace Application.Order.Features.Queries.GetOrderStatusById;

public record GetOrderStatusByIdQuery(int Id) : IRequest<ServiceResult<OrderStatusDto>>;