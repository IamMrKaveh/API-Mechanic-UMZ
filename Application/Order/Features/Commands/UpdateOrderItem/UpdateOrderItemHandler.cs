namespace Application.Order.Features.Commands.UpdateOrderItem;

public class UpdateOrderItemHandler : IRequestHandler<UpdateOrderItemCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(UpdateOrderItemCommand request, CancellationToken ct)
        => Task.FromResult(ServiceResult.Failure("Modifying placed order items is not allowed."));
}