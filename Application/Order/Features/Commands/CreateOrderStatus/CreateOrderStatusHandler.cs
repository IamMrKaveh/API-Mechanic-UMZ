using Application.Cache.Contracts;
using Application.Order.Features.Shared;
using Domain.Order.Entities;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.CreateOrderStatus;

public class CreateOrderStatusHandler(
	IOrderStatusRepository orderStatusRepository,
	IUnitOfWork unitOfWork,
	ICacheService cacheService)
	: ICommandHandler<CreateOrderStatusCommand, OrderStatusDto>
{
	public async Task<ServiceResult<OrderStatusDto>> Handle(
		CreateOrderStatusCommand request,
		CancellationToken ct)
	{
		var nameExists = await orderStatusRepository.ExistsByNameAsync(request.Name, null, ct);
		if (nameExists)
			return ServiceResult<OrderStatusDto>.Validation("ÙˆØ¶Ø¹ÛŒØªÛŒ Ø¨Ø§ Ø§ÛŒÙ† Ù†Ø§Ù… Ù‚Ø¨Ù„Ø§Ù‹ Ø«Ø¨Øª Ø´Ø¯Ù‡ Ø§Ø³Øª.");

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

		await cacheService.RemoveByPrefixAsync("order-status:", ct);

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
			IsActive = status.IsActive,
			IsDefault = status.IsDefault,
			RowVersion = status.RowVersion is { Length: > 0 } ? Convert.ToBase64String(status.RowVersion) : null
		};

		return ServiceResult<OrderStatusDto>.Success(dto);
	}
}