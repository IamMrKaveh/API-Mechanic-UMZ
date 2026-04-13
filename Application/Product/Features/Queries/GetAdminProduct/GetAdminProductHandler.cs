using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Queries.GetAdminProduct;

public sealed class GetAdminProductHandler(
    IProductQueryService productQueryService) : IRequestHandler<GetAdminProductQuery, ServiceResult<AdminProductDetailDto?>>
{
    public async Task<ServiceResult<AdminProductDetailDto?>> Handle(
        GetAdminProductQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var result = await productQueryService.GetAdminProductDetailAsync(productId, ct);

        if (result is null)
            return ServiceResult<AdminProductDetailDto?>.NotFound("محصول یافت نشد.");

        return ServiceResult<AdminProductDetailDto?>.Success(result);
    }
}