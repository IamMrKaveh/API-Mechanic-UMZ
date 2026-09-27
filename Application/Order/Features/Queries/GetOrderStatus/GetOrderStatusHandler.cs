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
        var statusResult = await (orderStatusQueryService.GetByIdAsync(request.Id, ct)).OrNotFoundAsync("وضعیت سفارش یافت نشد.");
        if (statusResult.IsFailure) return statusResult.Error;
        var status = statusResult.Value;

        return ServiceResult<OrderStatusDto>.Success(status);
    }
}