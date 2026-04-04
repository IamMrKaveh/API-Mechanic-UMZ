using Application.Common.Results;
using Application.Order.Contracts;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatusById;

public class GetOrderStatusByIdHandler(IOrderStatusQueryService orderStatusQueryService)
        : IRequestHandler<GetOrderStatusByIdQuery, ServiceResult<OrderStatusDto>>
{
    private readonly IOrderStatusQueryService _orderStatusQueryService = orderStatusQueryService;

    public async Task<ServiceResult<OrderStatusDto>> Handle(
        GetOrderStatusByIdQuery request,
        CancellationToken ct)
    {
        var dto = await _orderStatusQueryService.GetByIdAsync(request.Id, ct);

        if (dto is null)
            return ServiceResult<OrderStatusDto>.NotFound("وضعیت سفارش یافت نشد.");

        return ServiceResult<OrderStatusDto>.Success(dto);
    }
}