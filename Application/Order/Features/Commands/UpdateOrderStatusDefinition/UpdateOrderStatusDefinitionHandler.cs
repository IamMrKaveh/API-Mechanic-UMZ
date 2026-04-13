using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public class UpdateOrderStatusDefinitionHandler(
    IOrderStatusRepository orderStatusRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateOrderStatusDefinitionCommand, ServiceResult>
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
            request.DisplayName,
            request.Icon,
            request.Color,
            request.SortOrder,
            request.AllowCancel,
            request.AllowEdit);

        orderStatusRepository.Update(status);
        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}