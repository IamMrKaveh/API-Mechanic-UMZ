using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.RequestReturn;

public class RequestReturnHandler(
	IOrderRepository orderRepository,
	IUnitOfWork unitOfWork,
	INotificationService notificationService)
	: ICommandHandler<RequestReturnCommand>
{
	public async Task<ServiceResult> Handle(
		RequestReturnCommand request,
		CancellationToken ct)
	{
		var orderId = OrderId.From(request.OrderId);
		var order = await orderRepository.FindByIdAsync(orderId, ct);
		if (order is null)
			return ServiceResult.NotFound("Ø³ÙØ§Ø±Ø´ ÛŒØ§ÙØª Ù†Ø´Ø¯.");

		if (order.UserId != UserId.From(request.UserId))
			return ServiceResult.Unauthorized("Ø´Ù…Ø§ Ù…Ø¬Ø§Ø² Ø¨Ù‡ Ø¯Ø±Ø®ÙˆØ§Ø³Øª Ø¨Ø§Ø²Ú¯Ø´Øª Ø§ÛŒÙ† Ø³ÙØ§Ø±Ø´ Ù†ÛŒØ³ØªÛŒØ¯.");

		if (!string.IsNullOrEmpty(request.RowVersion))
			orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

		var oldStatusName = order.Status.DisplayName;

		try
		{
			order.MarkAsReturned();
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
				order.UserId,
				order.Id,
				oldStatusName,
				OrderStatusValue.Returned.DisplayName,
				ct);

			return ServiceResult.Success();
		}
		catch (ConcurrencyException)
		{
			return ServiceResult.Conflict("Ø§ÛŒÙ† Ø³ÙØ§Ø±Ø´ ØªÙˆØ³Ø· Ú©Ø§Ø±Ø¨Ø± Ø¯ÛŒÚ¯Ø±ÛŒ ØªØºÛŒÛŒØ± Ú©Ø±Ø¯Ù‡ Ø§Ø³Øª.");
		}
	}
}