using Application.Common.Results;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetAdminOrderById;

public record GetAdminOrderByIdQuery(Guid OrderId) : IRequest<ServiceResult<AdminOrderDto>>;