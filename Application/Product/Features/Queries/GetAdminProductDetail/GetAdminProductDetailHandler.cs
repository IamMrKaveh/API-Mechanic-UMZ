using Application.Common.Results;
using Application.Product.Contracts;
using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetAdminProductDetail;

public class GetAdminProductDetailHandler(IProductQueryService productQueryService)
        : IRequestHandler<GetAdminProductDetailQuery, ServiceResult<AdminProductDetailDto?>>
{
    private readonly IProductQueryService _productQueryService = productQueryService;

    public async Task<ServiceResult<AdminProductDetailDto?>> Handle(
        GetAdminProductDetailQuery request,
        CancellationToken ct)
    {
        var result = await _productQueryService.GetAdminProductDetailAsync(request.ProductId, ct);
        if (result is null)
            return ServiceResult<AdminProductDetailDto?>.NotFound("Product not found.");

        return ServiceResult<AdminProductDetailDto?>.Success(result);
    }
}