namespace Application.Product.Features.Commands.DeleteProduct;

public record DeleteProductCommand(
    Guid ProductId) : IRequest<ServiceResult>;