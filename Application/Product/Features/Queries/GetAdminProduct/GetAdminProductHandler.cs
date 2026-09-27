using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Queries.GetAdminProduct;

public sealed class GetAdminProductHandler(
    IProductQueryService productQueryService)
    : IQueryHandler<GetAdminProductQuery, AdminProductDetailDto?>
{
    public async Task<ServiceResult<AdminProductDetailDto?>> Handle(
        GetAdminProductQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var resultResult = await (productQueryService.GetAdminProductDetailAsync(productId, ct)).OrNotFoundAsync("محصول یافت نشد.");
        if (resultResult.IsFailure) return resultResult.Error;
        var result = resultResult.Value;

        return ServiceResult<AdminProductDetailDto?>.Success(result);
    }
}