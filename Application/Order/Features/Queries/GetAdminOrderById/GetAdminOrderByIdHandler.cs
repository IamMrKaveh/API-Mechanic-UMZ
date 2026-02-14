namespace Application.Order.Features.Queries.GetAdminOrderById;

public class GetAdminOrderByIdHandler : IRequestHandler<GetAdminOrderByIdQuery, ServiceResult<AdminOrderDto>>
{
    private readonly IOrderQueryService _orderQueryService;

    public GetAdminOrderByIdHandler(IOrderQueryService orderQueryService)
    {
        _orderQueryService = orderQueryService;
    }

    public async Task<ServiceResult<AdminOrderDto>> Handle(
        GetAdminOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _orderQueryService.GetAdminOrderDetailsAsync(
            request.OrderId,
            cancellationToken);

        if (order == null)
            return ServiceResult<AdminOrderDto>.Failure("سفارش یافت نشد.", 404);

        return ServiceResult<AdminOrderDto>.Success(order);
    }
}