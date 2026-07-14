using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.ApproveReturn;

public class ApproveReturnHandler(
	IOrderRepository orderRepository,
	IInventoryService inventoryService,
	IUnitOfWork unitOfWork,
	ICurrentUserService currentUserService)
	: ICommandHandler<ApproveReturnCommand>
{
	public async Task<ServiceResult> Handle(
		ApproveReturnCommand request,
		CancellationToken ct)
	{
		var orderId = OrderId.From(request.OrderId);
		var order = await orderRepository.FindByIdAsync(orderId, ct);
		var userId = UserId.From(currentUserService.UserId.Value);

		if (order is null)
			return ServiceResult.NotFound("Ø³ÙØ§Ø±Ø´ ÛŒØ§ÙØª Ù†Ø´Ø¯.");

		try
		{
			order.MarkAsReturned();
		}
		catch (DomainException ex)
		{
			return ServiceResult.Forbidden(ex.Message);
		}

		return await unitOfWork.ExecuteStrategyAsync(async cancellationToken =>
		{
			orderRepository.Update(order);
			await unitOfWork.SaveChangesAsync(cancellationToken);

			var result = await inventoryService.ReturnStockForOrderAsync(
				orderId,
				userId.Value,
				request.Reason,
				cancellationToken);

			if (!result.IsSuccess)
				return ServiceResult.Failure($"Ø®Ø·Ø§ Ø¯Ø± Ø¨Ø§Ø²Ú¯Ø´Øª Ù…ÙˆØ¬ÙˆØ¯ÛŒ: {result.Error}");

			return ServiceResult.Success();
		}, ct);
	}
}