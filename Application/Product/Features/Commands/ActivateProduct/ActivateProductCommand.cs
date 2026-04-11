namespace Application.Product.Features.Commands.ActivateProduct;

public record ActivateProductCommand(
    Guid ProductId,
    Guid UserId) : IRequest<ServiceResult>;