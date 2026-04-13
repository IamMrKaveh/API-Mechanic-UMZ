using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Product.Features.Commands.ActivateProduct;

public sealed class ActivateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<ActivateProductCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        ActivateProductCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);
        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult.NotFound("محصول یافت نشد.");

        if (product.IsActive)
            return ServiceResult.Conflict("محصول از قبل فعال است.");

        product.Activate();
        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            product.Id,
            "ActivateProduct",
            $"محصول '{product.Name}' فعال شد.",
            UserId.From(request.ActivatedByUserId));

        return ServiceResult.Success();
    }
}