using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatusById;

public class GetOrderStatusByIdHandler(IOrderStatusQueryService orderStatusQueryService)
        : IRequestHandler<GetOrderStatusByIdQuery, ServiceResult<OrderStatusDto>>
{
    public async Task<ServiceResult<OrderStatusDto>> Handle(
        GetOrderStatusByIdQuery request,
        CancellationToken ct)
    {
        var dto = await orderStatusQueryService.GetByIdAsync(request.Id, ct);

        if (dto is null)
            return ServiceResult<OrderStatusDto>.NotFound("وضعیت سفارش یافت نشد.");

        return ServiceResult<OrderStatusDto>.Success(dto);
    }
}