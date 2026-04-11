namespace Application.Product.Features.Commands.DeactivateProduct;

public record DeactivateProductCommand(
    Guid ProductId,
    Guid DeactivatedByUserId) : IRequest<ServiceResult>;