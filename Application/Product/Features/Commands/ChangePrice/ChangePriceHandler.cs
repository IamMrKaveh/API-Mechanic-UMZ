using Domain.Product.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Product.Features.Commands.ChangePrice;

public sealed class ChangePriceHandler(
    IVariantRepository variantRepository,
    ICacheService cacheService)
    : ICommandHandler<ChangePriceCommand>
{
    public async Task<ServiceResult> Handle(
        ChangePriceCommand request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var productId = ProductId.From(request.ProductId);

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

        await cacheService.RemoveAsync($"product:{request.ProductId}", ct);
        await cacheService.RemoveAsync($"variant:{request.VariantId}", ct);

        return ServiceResult.Success();
    }
}