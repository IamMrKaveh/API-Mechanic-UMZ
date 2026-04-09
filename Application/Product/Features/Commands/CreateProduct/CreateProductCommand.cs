using Application.Product.Features.Shared;

namespace Application.Product.Features.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Slug,
    string Description,
    decimal Price,
    Guid CategoryId,
    Guid BrandId,
    Guid CreatedByUserId) : IRequest<ServiceResult<ProductDetailDto>>;