using Application.Product.Features.Shared;

namespace Application.Product.Features.Commands.CreateProduct;

public record CreateProductCommand(
    Guid CategoryId,
    Guid BrandId,
    string Name) : ICommand<ProductDetailDto>;