using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Queries.GetProductDetails;

public sealed class GetProductDetailsHandler(
    IProductQueryService productQueryService) : IRequestHandler<GetProductDetailsQuery, ServiceResult<PublicProductDetailDto?>>
{
    public async Task<ServiceResult<PublicProductDetailDto?>> Handle(
        GetProductDetailsQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var result = await productQueryService.GetPublicProductDetailAsync(productId, ct);
        if (result is null)
            return ServiceResult<PublicProductDetailDto?>.NotFound("محصول یافت نشد.");

        return ServiceResult<PublicProductDetailDto?>.Success(result);
    }
}