using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Exceptions;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public class UpdateOrderStatusDefinitionHandler(
    IOrderStatusRepository orderStatusRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICacheService cacheService)
    : ICommandHandler<UpdateOrderStatusDefinitionCommand>
{
    public async Task<ServiceResult> Handle(
        UpdateOrderStatusDefinitionCommand request,
        CancellationToken ct)
    {
        var orderStatusId = OrderStatusId.From(request.Id);
        var status = await orderStatusRepository.GetByIdAsync(orderStatusId, ct);
        if (status is null)
            return ServiceResult.NotFound("وضعیت یافت نشد.");

        if (!string.IsNullOrEmpty(request.RowVersion))
        {
            try
            {
                orderStatusRepository.SetOriginalRowVersion(status, Convert.FromBase64String(request.RowVersion));
            }
            catch (FormatException)
            {
                return ServiceResult.Validation("RowVersion نامعتبر است.");
            }
        }

        status.Update(
            request.DisplayName,
            request.Icon,
            request.Color,
            request.SortOrder,
            request.AllowCancel,
            request.AllowEdit);

        orderStatusRepository.Update(status);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این وضعیت توسط کاربر دیگری تغییر کرده است.");
        }

        await cacheService.RemoveByPrefixAsync("order-status:", ct);

        await auditService.LogSystemEventAsync(
            "OrderStatusUpdated",
            $"وضعیت سفارش {status.Name} ویرایش شد.",
            ct);

        return ServiceResult.Success();
    }
}