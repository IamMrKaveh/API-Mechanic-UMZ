namespace Application.Order.Features.Queries.GetOrderStatusById;

public class GetOrderStatusByIdHandler : IRequestHandler<GetOrderStatusByIdQuery, ServiceResult<OrderStatusDto>>
{
    private readonly IOrderStatusRepository _repo;

    public GetOrderStatusByIdHandler(IOrderStatusRepository repo) => _repo = repo;

    public async Task<ServiceResult<OrderStatusDto>> Handle(GetOrderStatusByIdQuery request, CancellationToken ct)
    {
        var status = await _repo.GetByIdAsync(request.Id, ct);
        if (status == null) return ServiceResult<OrderStatusDto>.Failure("NotFound");
        return ServiceResult<OrderStatusDto>.Success(new OrderStatusDto { Id = status.Id, Name = status.Name, DisplayName = status.DisplayName });
    }
}