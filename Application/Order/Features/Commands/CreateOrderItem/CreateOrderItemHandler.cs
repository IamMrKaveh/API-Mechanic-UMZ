namespace Application.Order.Features.Commands.CreateOrderItem;

public class CreateOrderItemHandler : IRequestHandler<CreateOrderItemCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(CreateOrderItemCommand request, CancellationToken ct)
        => Task.FromResult(ServiceResult.Failure("Direct order item creation is not allowed. Manipulate cart and checkout."));
}