using Domain.Product.ValueObjects;

namespace Application.Product.Features.Queries.GetAdminProductDetail;

public class GetAdminProductDetailHandler(IProductQueryService productQueryService)
        : IRequestHandler<GetAdminProductDetailQuery, ServiceResult<AdminProductDetailDto?>>
{
    public async Task<ServiceResult<AdminProductDetailDto?>> Handle(
        GetAdminProductDetailQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.Id);

        var result = await productQueryService.GetAdminProductDetailAsync(productId, ct);
        if (result is null)
            return ServiceResult<AdminProductDetailDto?>.NotFound("Product not found.");

        return ServiceResult<AdminProductDetailDto?>.Success(result);
    }
}