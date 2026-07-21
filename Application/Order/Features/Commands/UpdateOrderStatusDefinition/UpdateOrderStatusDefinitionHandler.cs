using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public class UpdateOrderStatusDefinitionHandler(
    IOrderStatusRepository orderStatusRepository,
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

        byte[]? rowVersion = null;
        if (!string.IsNullOrEmpty(request.RowVersion))
        {
            try
            {
                rowVersion = Convert.FromBase64String(request.RowVersion);
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

        orderStatusRepository.Update(status, rowVersion);

        await cacheService.RemoveByPrefixAsync("order-status:", ct);

        await auditService.LogSystemEventAsync(
            "OrderStatusUpdated",
            $"وضعیت سفارش {status.Name} ویرایش شد.",
            ct);

        return ServiceResult.Success();
    }
}
