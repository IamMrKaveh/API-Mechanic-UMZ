using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public class UpdateOrderStatusDefinitionHandler(
    IOrderStatusRepository orderStatusRepository,
    IAuditService auditService)
    : ICommandHandler<UpdateOrderStatusDefinitionCommand>
{
    public async Task<ServiceResult> Handle(
        UpdateOrderStatusDefinitionCommand request,
        CancellationToken ct)
    {
        var orderStatusId = OrderStatusId.From(request.Id);
        var statusResult = await (orderStatusRepository.GetByIdAsync(orderStatusId, ct)).OrNotFoundAsync("وضعیت یافت نشد.");
        if (statusResult.IsFailure) return statusResult.Error;
        var status = statusResult.Value;

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


        await auditService.LogSystemEventAsync(
            "OrderStatusUpdated",
            $"وضعیت سفارش {status.Name} ویرایش شد.",
            ct);

        return ServiceResult.Success();
    }
}
