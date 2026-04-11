namespace Application.Product.Features.Commands.DeactivateProduct;

public record DeactivateProductCommand(
    Guid ProductId,
    Guid UserId) : IRequest<ServiceResult>;