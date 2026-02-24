namespace Application.Product.Features.Commands.DeactivateProduct;

public record DeactivateProductCommand(int ProductId) : IRequest<ServiceResult>;