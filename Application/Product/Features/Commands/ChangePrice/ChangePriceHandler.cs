using Domain.Common.Exceptions;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Product.Features.Commands.ChangePrice;

public class ChangePriceHandler(
    IVariantRepository variantRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICacheService cacheService) : IRequestHandler<ChangePriceCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        ChangePriceCommand request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var productId = ProductId.From(request.ProductId);
        var userId = UserId.From(request.ProductId);

        var variant = await variantRepository.GetByIdAsync(variantId, ct);
        if (variant is null || variant.ProductId != productId)
            return ServiceResult.NotFound("Variant not found.");

        try
        {
            variant.SetPricing(request.PurchasePrice, request.SellingPrice, request.OriginalPrice);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        variantRepository.Update(variant);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            productId,
            "ChangePrice",
            $"Variant {request.VariantId} prices changed. Selling: {request.SellingPrice}, Original: {request.OriginalPrice}",
            userId);

        await cacheService.RemoveAsync($"product:{request.ProductId}", ct);
        await cacheService.RemoveAsync($"variant:{request.VariantId}", ct);

        return ServiceResult.Success();
    }
}