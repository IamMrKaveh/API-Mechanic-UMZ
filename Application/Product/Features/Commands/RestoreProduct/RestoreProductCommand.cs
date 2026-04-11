namespace Application.Product.Features.Commands.RestoreProduct;

public record RestoreProductCommand(
    Guid ProductId,
    Guid UserId) : IRequest<ServiceResult>;