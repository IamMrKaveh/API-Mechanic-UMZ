using Application.Cache.Contracts;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.DeactivateOrderStatus;

public class DeactivateOrderStatusHandler(
	IOrderStatusRepository orderStatusRepository,
	IUnitOfWork unitOfWork,
	ICacheService cacheService)
	: ICommandHandler<DeactivateOrderStatusCommand>
{
	public async Task<ServiceResult> Handle(
		DeactivateOrderStatusCommand request,
		CancellationToken ct)
	{
		var statusId = OrderStatusId.From(request.Id);
		var status = await orderStatusRepository.GetByIdAsync(statusId, ct);
		if (status is null)
			return ServiceResult.NotFound("ÙˆØ¶Ø¹ÛŒØª Ø³ÙØ§Ø±Ø´ ÛŒØ§ÙØª Ù†Ø´Ø¯.");

		if (!status.IsActive)
			return ServiceResult.Success();

		try
		{
			status.Deactivate();
		}
		catch (DomainException ex)
		{
			return ServiceResult.Failure(ex.Message);
		}

		orderStatusRepository.Update(status);
		await unitOfWork.SaveChangesAsync(ct);

		await cacheService.RemoveByPrefixAsync("order-status:", ct);

		return ServiceResult.Success();
	}
}