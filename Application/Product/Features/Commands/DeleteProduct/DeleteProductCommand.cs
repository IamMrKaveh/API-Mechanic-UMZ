namespace Application.Product.Features.Commands.DeleteProduct;

public record DeleteProductCommand(
    Guid ProductId,
    Guid DeletedByUserId) : IRequest<ServiceResult>;