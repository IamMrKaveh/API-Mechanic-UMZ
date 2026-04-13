using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Product.Features.Commands.ChangePrice;

public sealed class ChangePriceHandler(
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
        var userId = UserId.From(request.UserId);

        var variant = await variantRepository.GetByIdAsync(variantId, ct);
        if (variant is null || variant.ProductId != productId)
            return ServiceResult.NotFound("واریانت یافت نشد.");

        try
        {
            var newPrice = Money.Create(request.SellingPrice);
            var compareAtPrice = request.OriginalPrice > request.SellingPrice
                ? Money.Create(request.OriginalPrice)
                : null;

            variant.ChangePrice(newPrice, compareAtPrice);
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
            $"قیمت واریانت {request.VariantId} تغییر کرد. قیمت فروش: {request.SellingPrice}, قیمت اصلی: {request.OriginalPrice}",
            userId);

        await cacheService.RemoveAsync($"product:{request.ProductId}", ct);
        await cacheService.RemoveAsync($"variant:{request.VariantId}", ct);

        return ServiceResult.Success();
    }
}