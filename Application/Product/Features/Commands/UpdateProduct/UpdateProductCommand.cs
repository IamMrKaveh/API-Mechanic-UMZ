using Application.Common.Results;
using Application.Product.Features.Shared;

namespace Application.Product.Features.Commands.UpdateProduct;

public record UpdateProductCommand(
    int Id,
    string Name,
    string? Slug,
    string Description,
    int CategoryId,
    int BrandId,
    bool IsActive,
    bool IsFeatured,
    string RowVersion,
    int UpdatedByUserId) : IRequest<ServiceResult<ProductDetailDto>>;