using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Product.Features.Commands.DeactivateProduct;

public sealed class DeactivateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<DeactivateProductCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        DeactivateProductCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);
        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult.NotFound("محصول یافت نشد.");

        if (!product.IsActive)
            return ServiceResult.Conflict("محصول از قبل غیرفعال است.");

        product.Deactivate();
        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            product.Id,
            "DeactivateProduct",
            $"محصول '{product.Name}' غیرفعال شد.",
            UserId.From(request.DeactivatedByUserId));

        return ServiceResult.Success();
    }
}