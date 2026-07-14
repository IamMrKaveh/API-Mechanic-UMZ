using Application.Cache.Contracts;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.DeleteOrderStatus;

public class DeleteOrderStatusHandler(
	IOrderStatusRepository orderStatusRepository,
	IUnitOfWork unitOfWork,
	ICacheService cacheService)
	: ICommandHandler<DeleteOrderStatusCommand>
{
	public async Task<ServiceResult> Handle(DeleteOrderStatusCommand request, CancellationToken ct)
	{
		var statusId = OrderStatusId.From(request.Id);
		var status = await orderStatusRepository.GetByIdAsync(statusId, ct);
		if (status is null)
			return ServiceResult.NotFound("ÙˆØ¶Ø¹ÛŒØª Ø³ÙØ§Ø±Ø´ ÛŒØ§ÙØª Ù†Ø´Ø¯.");

		if (status.IsDefault)
			return ServiceResult.Forbidden("Ø§Ù…Ú©Ø§Ù† Ø­Ø°Ù ÙˆØ¶Ø¹ÛŒØª Ù¾ÛŒØ´â€ŒÙØ±Ø¶ ÙˆØ¬ÙˆØ¯ Ù†Ø¯Ø§Ø±Ø¯.");

		var isUsed = await orderStatusRepository.IsInUseAsync(statusId, ct);
		if (isUsed)
			return ServiceResult.Forbidden("Ø§Ù…Ú©Ø§Ù† Ø­Ø°Ù ÙˆØ¶Ø¹ÛŒØªÛŒ Ú©Ù‡ Ø¨Ù‡ Ø³ÙØ§Ø±Ø´Ø§Øª Ø§Ø®ØªØµØ§Øµ Ø¯Ø§Ø¯Ù‡ Ø´Ø¯Ù‡ ÙˆØ¬ÙˆØ¯ Ù†Ø¯Ø§Ø±Ø¯.");

		status.MarkAsDeleted();
		orderStatusRepository.Remove(status);
		await unitOfWork.SaveChangesAsync(ct);

		await cacheService.RemoveByPrefixAsync("order-status:", ct);

		return ServiceResult.Success();
	}
}