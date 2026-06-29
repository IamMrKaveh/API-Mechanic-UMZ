using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatus;

public class GetOrderStatusHandler(
    IOrderStatusQueryService orderStatusQueryService)
    : IQueryHandler<GetOrderStatusQuery, OrderStatusDto>
{
    public async Task<ServiceResult<OrderStatusDto>> Handle(
        GetOrderStatusQuery request,
        CancellationToken ct)
    {
        var status = await orderStatusQueryService.GetByIdAsync(request.Id, ct);

        if (status is null)
            return ServiceResult<OrderStatusDto>.NotFound("وضعیت سفارش یافت نشد.");

        return ServiceResult<OrderStatusDto>.Success(status);
    }
}