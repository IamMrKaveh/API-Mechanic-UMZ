using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Commands.RestoreProduct;

public class RestoreProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : ICommandHandler<RestoreProductCommand>
{
    public async Task<ServiceResult> Handle(
        RestoreProductCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult.NotFound("Product not found.");

        product.Restore();

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveAsync($"product:{request.ProductId}", ct);
        await cacheService.RemoveAsync($"brand:{product.BrandId}", ct);

        return ServiceResult.Success();
    }
}