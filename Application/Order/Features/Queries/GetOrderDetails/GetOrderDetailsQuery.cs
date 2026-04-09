using Application.Common.Results;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderDetails;

public record GetOrderDetailsQuery(Guid OrderId, Guid UserId) : IRequest<ServiceResult<OrderDto>>;