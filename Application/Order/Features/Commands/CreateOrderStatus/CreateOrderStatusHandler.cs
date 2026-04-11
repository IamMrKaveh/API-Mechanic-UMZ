using Application.Order.Features.Shared;
using Domain.Order.Entities;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.CreateOrderStatus;

public class CreateOrderStatusHandler(
    IOrderStatusRepository orderStatusRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateOrderStatusHandler> logger) : IRequestHandler<CreateOrderStatusCommand, ServiceResult<OrderStatusDto>>
{
    public async Task<ServiceResult<OrderStatusDto>> Handle(
        CreateOrderStatusCommand request,
        CancellationToken ct)
    {
        var existing = await _orderStatusRepository.GetByNameAsync(request.Name, ct);
        if (existing is not null)
            return ServiceResult<OrderStatusDto>.Conflict("وضعیت سفارش با این نام قبلاً وجود دارد.");

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

        logger.LogInformation(
            "New order status created: {StatusName} (ID: {StatusId})",
            status.Name, status.Id);

        var dto = new OrderStatusDto
        {
            Id = status.Id,
            Name = status.Name,
            DisplayName = status.DisplayName,
            Icon = status.Icon,
            Color = status.Color,
        };

        return ServiceResult<OrderStatusDto>.Success(dto);
    }
}