using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetAdminOrders;

public record GetAdminOrdersQuery(
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    bool? IsPaid,
    int Page,
    int PageSize)
    : IPageQuery<AdminOrderDto>;