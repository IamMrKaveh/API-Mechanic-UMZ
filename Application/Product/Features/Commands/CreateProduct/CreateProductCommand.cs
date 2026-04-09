using Application.Product.Features.Shared;

namespace Application.Product.Features.Commands.CreateProduct;

public record CreateProductCommand(
    Guid CategoryId,
    Guid BrandId,
    Guid CreatedByUserId,
    string Name,
    string Description,
    decimal Price,
    string? Slug) : IRequest<ServiceResult<ProductDetailDto>>;