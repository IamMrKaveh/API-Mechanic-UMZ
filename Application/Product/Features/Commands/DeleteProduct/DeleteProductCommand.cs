namespace Application.Product.Features.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id, Guid DeletedByUserId) : IRequest<ServiceResult>;