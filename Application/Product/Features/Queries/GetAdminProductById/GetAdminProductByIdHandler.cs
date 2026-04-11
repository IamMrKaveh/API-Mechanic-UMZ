using Domain.Product.ValueObjects;

namespace Application.Product.Features.Queries.GetAdminProductById;

public class GetAdminProductByIdHandler(IProductQueryService productQueryService) : IRequestHandler<GetAdminProductByIdQuery, ServiceResult<AdminProductDetailDto?>>
{
    public async Task<ServiceResult<AdminProductDetailDto?>> Handle(
        GetAdminProductByIdQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var result = await productQueryService.GetAdminProductDetailAsync(
            productId, ct);

        if (result is null)
            return ServiceResult<AdminProductDetailDto?>.NotFound("Product not found.");

        return ServiceResult<AdminProductDetailDto?>.Success(result);
    }
}