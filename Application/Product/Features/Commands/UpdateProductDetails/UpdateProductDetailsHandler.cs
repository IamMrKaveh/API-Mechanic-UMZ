using Application.Common.Interfaces;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Product.Features.Commands.UpdateProductDetails;

public class UpdateProductDetailsHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateProductDetailsCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(UpdateProductDetailsCommand request, CancellationToken ct)
    {
        var productId = ProductId.From(request.Id);
        var userId = UserId.From(currentUserService.CurrentUser.UserId);

        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null) return ServiceResult.NotFound("Product not found.");

        productRepository.SetOriginalRowVersion(product, Convert.FromBase64String(request.RowVersion));

        if (!string.IsNullOrEmpty(request.Sku)
            && await productRepository.ExistsBySkuAsync(request.Sku, productId, ct))
            return ServiceResult.Conflict("Product SKU already exists.");

        product.UpdateDetails(
            request.Name,
            request.Description != null ? request.Description : null,
            request.Sku,
            request.BrandId,
            request.IsActive);

        productRepository.Update(product);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            await auditService.LogProductEventAsync(
                productId,
                "UpdateProductDetails",
                "Product details updated.",
                userId);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("This product was modified by another user. Please refresh and try again.");
        }
    }
}