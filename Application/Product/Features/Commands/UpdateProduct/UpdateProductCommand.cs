using Application.Product.Features.Shared;

namespace Application.Product.Features.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    decimal Price,
    string? Slug,
    string? Description,
    Guid CategoryId,
    Guid BrandId,
    bool IsActive,
    bool IsFeatured,
    string RowVersion,
    Guid UpdatedByUserId) : IRequest<ServiceResult<ProductDetailDto>>;