using Application.Product.Features.Shared;

namespace Application.Product.Features.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    Guid CategoryId,
    Guid BrandId,
    string Name,
    string? Slug,
    string? Description,
    bool IsActive,
    bool IsFeatured,
    string RowVersion) : ICommand<ProductDetailDto>;