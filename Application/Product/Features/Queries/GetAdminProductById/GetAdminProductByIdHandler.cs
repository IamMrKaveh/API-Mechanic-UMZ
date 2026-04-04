using Application.Common.Results;
using Application.Product.Contracts;
using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetAdminProductById;

public class GetAdminProductByIdHandler(IProductQueryService productQueryService) : IRequestHandler<GetAdminProductByIdQuery, ServiceResult<AdminProductDetailDto?>>
{
    private readonly IProductQueryService _productQueryService = productQueryService;

    public async Task<ServiceResult<AdminProductDetailDto?>> Handle(
        GetAdminProductByIdQuery request,
        CancellationToken ct)
    {
        var result = await _productQueryService.GetAdminProductDetailAsync(
            request.ProductId, ct);

        if (result == null)
            return ServiceResult<AdminProductDetailDto?>.NotFound("Product not found.");

        return ServiceResult<AdminProductDetailDto?>.Success(result);
    }
}