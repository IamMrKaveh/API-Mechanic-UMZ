using Application.Common.Results;
using Application.Product.Contracts;
using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProductById;

public class GetProductByIdHandler(IProductQueryService productQueryService)
        : IRequestHandler<GetProductByIdQuery, ServiceResult<PublicProductDetailDto?>>
{
    private readonly IProductQueryService _productQueryService = productQueryService;

    public async Task<ServiceResult<PublicProductDetailDto?>> Handle(
        GetProductByIdQuery request,
        CancellationToken ct)
    {
        var result = await _productQueryService.GetPublicProductDetailAsync(
            request.Id, ct);

        if (result == null)
            return ServiceResult<PublicProductDetailDto?>.NotFound("Product not found.");

        return ServiceResult<PublicProductDetailDto?>.Success(result);
    }
}