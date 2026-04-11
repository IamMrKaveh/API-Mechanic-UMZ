namespace Application.Product.Features.Commands.UpdateProductDetails;

public record UpdateProductDetailsCommand(
    Guid ProductId,
    string Name,
    string? Description,
    Guid BrandId,
    bool IsActive,
    string? Sku,
    string RowVersion,
    Guid UserId) : IRequest<ServiceResult>;