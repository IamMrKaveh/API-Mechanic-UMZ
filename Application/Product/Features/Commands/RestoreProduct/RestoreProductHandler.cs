using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Product.Features.Commands.RestoreProduct;

public class RestoreProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICacheService cacheService) : IRequestHandler<RestoreProductCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        RestoreProductCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.Id);
        var userId = UserId.From(request.UserId);

        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult.NotFound("Product not found.");

        product.Restore();

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            productId,
            "RestoreProduct", $"Product '{product.Name}' restored.",
            userId);

        await cacheService.ClearAsync($"product:{request.Id}", ct);
        await cacheService.ClearAsync($"brand:{product.BrandId}", ct);

        return ServiceResult.Success();
    }
}