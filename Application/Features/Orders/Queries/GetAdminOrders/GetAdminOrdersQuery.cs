namespace Application.Features.Orders.Queries.GetAdminOrders;

public record GetAdminOrdersQuery(int? UserId, int? StatusId, DateTime? FromDate, DateTime? ToDate, int Page, int PageSize) : IRequest<ServiceResult<PagedResultDto<object>>>;

public class GetAdminOrdersQueryHandler : IRequestHandler<GetAdminOrdersQuery, ServiceResult<PagedResultDto<object>>>
{
    private readonly IAdminOrderService _orderService;

    public GetAdminOrdersQueryHandler(IAdminOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<ServiceResult<PagedResultDto<object>>> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        var (orders, totalItems) = await _orderService.GetOrdersAsync(
            request.UserId,
            request.StatusId,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.PageSize);

        var pagedResult = PagedResultDto<object>.Create(orders, totalItems, request.Page, request.PageSize);
        return ServiceResult<PagedResultDto<object>>.Ok(pagedResult);
    }
}