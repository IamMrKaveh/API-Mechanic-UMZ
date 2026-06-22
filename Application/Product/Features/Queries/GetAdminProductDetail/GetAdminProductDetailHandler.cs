using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Queries.GetAdminProductDetail;

public sealed class GetAdminProductDetailHandler(
    IProductQueryService productQueryService)
    : IQueryHandler<GetAdminProductDetailQuery, AdminProductDetailDto?>
{
    public async Task<ServiceResult<AdminProductDetailDto?>> Handle(
        GetAdminProductDetailQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var result = await productQueryService.GetAdminProductDetailAsync(productId, ct);
        if (result is null)
            return ServiceResult<AdminProductDetailDto?>.NotFound("محصول یافت نشد.");

        return ServiceResult<AdminProductDetailDto?>.Success(result);
    }
}