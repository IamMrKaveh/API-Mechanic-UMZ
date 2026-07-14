using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrderStatus;

public class UpdateOrderStatusHandler(
	IOrderRepository orderRepository,
	IUnitOfWork unitOfWork,
	INotificationService notificationService)
	: ICommandHandler<UpdateOrderStatusCommand>
{
	public async Task<ServiceResult> Handle(
		UpdateOrderStatusCommand request,
		CancellationToken ct)
	{
		var orderId = OrderId.From(request.OrderId);
		var order = await orderRepository.FindByIdAsync(orderId, ct);
		if (order is null)
			return ServiceResult.NotFound("Ø³ÙØ§Ø±Ø´ ÛŒØ§ÙØª Ù†Ø´Ø¯.");

		if (!string.IsNullOrEmpty(request.RowVersion))
			orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

		OrderStatusValue newStatus;
		try
		{
			newStatus = OrderStatusValue.From(request.NewStatus);
		}
		catch (DomainException)
		{
			return ServiceResult.Failure("ÙˆØ¶Ø¹ÛŒØª Ø³ÙØ§Ø±Ø´ Ù†Ø§Ù…Ø¹ØªØ¨Ø± Ø§Ø³Øª.");
		}

		if (!order.Status.CanTransitionTo(newStatus))
			return ServiceResult.Validation($"Ø§Ù†ØªÙ‚Ø§Ù„ Ø¨Ù‡ ÙˆØ¶Ø¹ÛŒØª '{newStatus.DisplayName}' Ù…Ø¬Ø§Ø² Ù†ÛŒØ³Øª.");

		var oldStatusName = order.Status.DisplayName;

		try
		{
			switch (newStatus.Value)
			{
				case "Pending": order.MoveToPending(); break;
				case "Processing": order.StartProcessing(); break;
				case "Shipped": order.MarkAsShipped(); break;
				case "Delivered": order.MarkAsDelivered(); break;
				case "Returned": order.MarkAsReturned(); break;
				case "Refunded": order.Refund(); break;
				case "Cancelled":
					return ServiceResult.Validation("Ø¨Ø±Ø§ÛŒ Ù„ØºÙˆ Ø³ÙØ§Ø±Ø´ Ø§Ø² Ù…Ø³ÛŒØ± Ø§Ø®ØªØµØ§ØµÛŒ Ù„ØºÙˆ Ø§Ø³ØªÙØ§Ø¯Ù‡ Ú©Ù†ÛŒØ¯.");
				default:
					return ServiceResult.Forbidden($"ØªØºÛŒÛŒØ± Ù…Ø³ØªÙ‚ÛŒÙ… Ø¨Ù‡ ÙˆØ¶Ø¹ÛŒØª '{newStatus.DisplayName}' Ù…Ø¬Ø§Ø² Ù†ÛŒØ³Øª.");
			}
		}
		catch (DomainException ex)
		{
			return ServiceResult.Failure(ex.Message);
		}

		orderRepository.Update(order);

		try
		{
			await unitOfWork.SaveChangesAsync(ct);

			await notificationService.SendOrderStatusNotificationAsync(
				order.UserId, order.Id, oldStatusName, newStatus.DisplayName, ct);

			return ServiceResult.Success();
		}
		catch (ConcurrencyException)
		{
			return ServiceResult.Conflict("Ø§ÛŒÙ† Ø³ÙØ§Ø±Ø´ ØªÙˆØ³Ø· Ú©Ø§Ø±Ø¨Ø± Ø¯ÛŒÚ¯Ø±ÛŒ ØªØºÛŒÛŒØ± Ú©Ø±Ø¯Ù‡ Ø§Ø³Øª.");
		}
	}
}