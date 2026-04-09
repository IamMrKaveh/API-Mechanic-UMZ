using Application.Common.Results;
using Application.Order.Contracts;
using Application.Order.Features.Shared;
using SharedKernel.Models;

namespace Application.Order.Features.Queries.GetUserOrders;

public class GetUserOrdersHandler(IOrderQueryService orderQueryService) : IRequestHandler<GetUserOrdersQuery, ServiceResult<PaginatedResult<OrderDto>>>
{
    private readonly IOrderQueryService _orderQueryService = orderQueryService;

    public async Task<ServiceResult<PaginatedResult<OrderDto>>> Handle(
        GetUserOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _orderQueryService.GetUserOrdersAsync(
            request.UserId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<OrderDto>>.Success(result);
    }
}