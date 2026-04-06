using Application.Common.Results;
using Application.Product.Features.Shared;

namespace Application.Product.Features.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Slug,
    string Description,
    int CategoryId,
    int BrandId,
    int CreatedByUserId) : IRequest<ServiceResult<ProductDetailDto>>;