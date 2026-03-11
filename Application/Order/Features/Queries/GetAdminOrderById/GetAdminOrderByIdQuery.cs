using Application.Common.Models;

namespace Application.Order.Features.Queries.GetAdminOrderById;

public record GetAdminOrderByIdQuery(int OrderId) : IRequest<ServiceResult<AdminOrderDto>>;