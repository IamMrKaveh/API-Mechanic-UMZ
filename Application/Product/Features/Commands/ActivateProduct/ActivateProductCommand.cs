namespace Application.Product.Features.Commands.ActivateProduct;

public record ActivateProductCommand(Guid ProductId) : IRequest<ServiceResult>;