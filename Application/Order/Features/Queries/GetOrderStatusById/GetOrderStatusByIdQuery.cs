using Application.Common.Results;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatusById;

public record GetOrderStatusByIdQuery(int Id) : IRequest<ServiceResult<OrderStatusDto>>;