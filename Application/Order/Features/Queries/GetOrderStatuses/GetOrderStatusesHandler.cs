namespace Application.Order.Features.Queries.GetOrderStatuses;

public class GetOrderStatusesHandler : IRequestHandler<GetOrderStatusesQuery, ServiceResult<IEnumerable<OrderStatusDto>>>
{
    private readonly IOrderStatusRepository _orderStatusRepository;

    public GetOrderStatusesHandler(IOrderStatusRepository orderStatusRepository)
    {
        _orderStatusRepository = orderStatusRepository;
    }

    public async Task<ServiceResult<IEnumerable<OrderStatusDto>>> Handle(
        GetOrderStatusesQuery request,
        CancellationToken cancellationToken)
    {
        var statuses = await _orderStatusRepository.GetAllAsync(cancellationToken);

        var dtos = statuses.Select(s => new OrderStatusDto
        {
            Id = s.Id,
            Name = s.Name,
            DisplayName = s.DisplayName,
            Icon = s.Icon,
            Color = s.Color,
        });

        return ServiceResult<IEnumerable<OrderStatusDto>>.Success(dtos);
    }
}