namespace Application.Product.Features.Commands.ActivateProduct;

public record ActivateProductCommand(
    Guid ProductId,
    Guid ActivatedByUserId) : IRequest<ServiceResult>;