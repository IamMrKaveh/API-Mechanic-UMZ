using Domain.Common.Exceptions;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Commands.RemoveVariant;

public class RemoveVariantHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<RemoveVariantCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        RemoveVariantCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);
        var userId = UserId.From(request.UserId);
        var variantId = VariantId.From(request.VariantId);

        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult.NotFound("Product not found.");

        try
        {
            product.RemoveVariant(variantId, userId);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            product.Id, "RemoveVariant",
            $"Variant {variantId} soft-deleted from product '{product.Name}'.",
            userId);

        return ServiceResult.Success();
    }
}