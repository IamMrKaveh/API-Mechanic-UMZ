using Application.Product.Contracts;
using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Queries.GetProduct;

public sealed class GetProductHandler(
    IProductQueryService productQueryService) : IRequestHandler<GetProductQuery, ServiceResult<ProductDetailDto>>
{
    public async Task<ServiceResult<ProductDetailDto>> Handle(
        GetProductQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.Id);
        var dto = await productQueryService.GetProductDetailAsync(productId, ct);

        return dto is null
            ? ServiceResult<ProductDetailDto>.NotFound("محصول یافت نشد.")
            : ServiceResult<ProductDetailDto>.Success(dto);
    }
}