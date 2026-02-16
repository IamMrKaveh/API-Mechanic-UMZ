namespace Application.Order.Features.Queries.GetOrderItemById;

public class GetOrderItemByIdHandler : IRequestHandler<GetOrderItemByIdQuery, ServiceResult<OrderItemDto>>
{
    private readonly IOrderRepository _orderRepo;

    public GetOrderItemByIdHandler(IOrderRepository orderRepo) => _orderRepo = orderRepo;

    public async Task<ServiceResult<OrderItemDto>> Handle(GetOrderItemByIdQuery request, CancellationToken ct)
        => ServiceResult<OrderItemDto>.Failure("Use GetOrderDetailsQuery instead.");
}