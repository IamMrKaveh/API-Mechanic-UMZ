using Application.Order.Features.Shared;
using Domain.Order.Entities;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.CreateOrderStatus;

public class CreateOrderStatusHandler(
    IOrderStatusRepository orderStatusRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateOrderStatusCommand, ServiceResult<OrderStatusDto>>
{
    public async Task<ServiceResult<OrderStatusDto>> Handle(
        CreateOrderStatusCommand request,
        CancellationToken ct)
    {
        var status = OrderStatus.Create(
            request.Name,
            request.DisplayName,
            request.Icon,
            request.Color,
            request.SortOrder,
            request.AllowCancel,
            request.AllowEdit);

        await orderStatusRepository.AddAsync(status, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = new OrderStatusDto
        {
            Id = status.Id.Value,
            Name = status.Name,
            DisplayName = status.DisplayName,
            Icon = status.Icon,
            Color = status.Color,
            SortOrder = status.SortOrder,
            AllowCancel = status.AllowCancel,
            AllowEdit = status.AllowEdit,
            IsActive = status.IsActive
        };

        return ServiceResult<OrderStatusDto>.Success(dto);
    }
}