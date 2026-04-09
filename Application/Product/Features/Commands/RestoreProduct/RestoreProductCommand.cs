namespace Application.Product.Features.Commands.RestoreProduct;

public record RestoreProductCommand(Guid Id, Guid UserId) : IRequest<ServiceResult>;