namespace Application.Product.Features.Commands.DeleteProduct;

public record DeleteProductCommand(int Id) : IRequest<ServiceResult>;