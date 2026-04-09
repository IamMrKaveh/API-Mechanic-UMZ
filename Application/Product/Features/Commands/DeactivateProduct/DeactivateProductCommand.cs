namespace Application.Product.Features.Commands.DeactivateProduct;

public record DeactivateProductCommand(Guid ProductId) : IRequest<ServiceResult>;