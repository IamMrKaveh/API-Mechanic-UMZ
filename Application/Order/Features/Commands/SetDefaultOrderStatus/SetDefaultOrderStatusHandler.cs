using Application.Cache.Contracts;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.SetDefaultOrderStatus;

public class SetDefaultOrderStatusHandler(
	IOrderStatusRepository orderStatusRepository,
	IUnitOfWork unitOfWork,
	ICacheService cacheService)
	: ICommandHandler<SetDefaultOrderStatusCommand>
{
	public async Task<ServiceResult> Handle(
		SetDefaultOrderStatusCommand request,
		CancellationToken ct)
	{
		var statusId = OrderStatusId.From(request.Id);
		var status = await orderStatusRepository.GetByIdAsync(statusId, ct);
		if (status is null)
			return ServiceResult.NotFound("ÙˆØ¶Ø¹ÛŒØª Ø³ÙØ§Ø±Ø´ ÛŒØ§ÙØª Ù†Ø´Ø¯.");

		if (!status.IsActive)
			return ServiceResult.Validation("ÙˆØ¶Ø¹ÛŒØª ØºÛŒØ±ÙØ¹Ø§Ù„ Ù†Ù…ÛŒâ€ŒØªÙˆØ§Ù†Ø¯ Ù¾ÛŒØ´â€ŒÙØ±Ø¶ Ø´ÙˆØ¯.");

		if (status.IsDefault)
			return ServiceResult.Success();

		var currentDefault = await orderStatusRepository.GetDefaultAsync(ct);
		if (currentDefault is not null && currentDefault.Id != status.Id)
		{
			currentDefault.UnsetAsDefault();
			orderStatusRepository.Update(currentDefault);
		}

		try
		{
			status.SetAsDefault();
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