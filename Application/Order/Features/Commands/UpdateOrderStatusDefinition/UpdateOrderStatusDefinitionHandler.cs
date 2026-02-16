namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public class UpdateOrderStatusDefinitionHandler : IRequestHandler<UpdateOrderStatusDefinitionCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(UpdateOrderStatusDefinitionCommand request, CancellationToken ct)
        => Task.FromResult(ServiceResult.Success());
}