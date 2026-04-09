namespace Application.Product.Features.Commands.UpdateProductDetails;

public record UpdateProductDetailsCommand(
    Guid Id,
    string Name,
    string? Description,
    Guid BrandId,
    bool IsActive,
    string? Sku,
    string RowVersion) : IRequest<ServiceResult>;