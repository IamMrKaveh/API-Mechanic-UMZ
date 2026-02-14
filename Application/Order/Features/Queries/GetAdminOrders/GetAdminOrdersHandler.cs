namespace Application.Order.Features.Queries.GetAdminOrders;

public class GetAdminOrdersHandler : IRequestHandler<GetAdminOrdersQuery, ServiceResult<PaginatedResult<AdminOrderDto>>>
{
    private readonly IOrderQueryService _orderQueryService;

    public GetAdminOrdersHandler(IOrderQueryService orderQueryService)
    {
        _orderQueryService = orderQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<AdminOrderDto>>> Handle(
        GetAdminOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _orderQueryService.GetAdminOrdersAsync(
            request.UserId,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.IsPaid,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<AdminOrderDto>>.Success(result);
    }
}