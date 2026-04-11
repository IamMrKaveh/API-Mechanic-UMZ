using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public class UpdateOrderStatusDefinitionHandler(IOrderStatusRepository orderStatusRepository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateOrderStatusDefinitionCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateOrderStatusDefinitionCommand request,
        CancellationToken ct)
    {
        var orderStatusId = OrderStatusId.From(request.Id);
        var status = await orderStatusRepository.GetByIdAsync(orderStatusId, ct);
        if (status is null)
            return ServiceResult.NotFound("وضعیت یافت نشد.");

        status.Update(
            request.DisplayName ?? status.DisplayName,
            request.Icon ?? status.Icon,
            request.Color ?? status.Color,
            request.SortOrder ?? status.SortOrder,
            request.AllowCancel ?? status.AllowCancel,
            request.AllowEdit ?? status.AllowEdit
        );

        orderStatusRepository.Update(status);
        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}