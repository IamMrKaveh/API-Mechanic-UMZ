namespace Application.Order.Features.Commands.DeleteOrderItem;

public class DeleteOrderItemHandler : IRequestHandler<DeleteOrderItemCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(DeleteOrderItemCommand request, CancellationToken ct)
        => Task.FromResult(ServiceResult.Failure("Deleting placed order items is not allowed."));
}