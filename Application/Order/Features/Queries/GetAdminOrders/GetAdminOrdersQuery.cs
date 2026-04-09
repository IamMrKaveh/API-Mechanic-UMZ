using Application.Common.Results;
using Application.Order.Features.Shared;
using SharedKernel.Models;

namespace Application.Order.Features.Queries.GetAdminOrders;

public record GetAdminOrdersQuery(
    Guid? UserId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    bool? IsPaid,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<AdminOrderDto>>>;