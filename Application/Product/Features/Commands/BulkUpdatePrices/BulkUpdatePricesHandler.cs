using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Product.Features.Commands.BulkUpdatePrices;

public sealed class BulkUpdatePricesHandler(
    IVariantRepository variantRepository,
    IAuditService auditService,
    ICacheService cacheService)
    : ICommandHandler<BulkUpdatePricesCommand>
{
    public async Task<ServiceResult> Handle(BulkUpdatePricesCommand request, CancellationToken ct)
    {
        var updatesDict = request.Updates.ToDictionary(u => u.VariantId);
        var variantIds = updatesDict.Keys.Select(k => VariantId.From(k)).ToList();
        var variants = await variantRepository.GetByIdsAsync(variantIds, ct);

        var errors = new List<string>();
        var affectedProductIds = new HashSet<Guid>();
        var changesLog = new List<string>();

        foreach (var variant in variants)
        {
            if (updatesDict.TryGetValue(variant.Id.Value, out var update))
            {
                try
                {
                    var newPrice = Money.Create(update.SellingPrice);
                    var compareAtPrice = update.OriginalPrice > update.SellingPrice
                        ? Money.Create(update.OriginalPrice)
                        : null;

                    variant.ChangePrice(newPrice, compareAtPrice);
                    variantRepository.Update(variant);

                    changesLog.Add($"Variant {variant.Id.Value}: Selling={update.SellingPrice}");
                    affectedProductIds.Add(variant.ProductId.Value);
                }
                catch (DomainException ex)
                {
                    errors.Add($"Variant {variant.Id.Value}: {ex.Message}");
                }
            }
        }

        foreach (var productId in affectedProductIds)
        {
            await cacheService.RemoveAsync($"product:{productId}", ct);
        }

        await auditService.LogSystemEventAsync(
            "BulkPriceUpdate",
            $"بروزرسانی گروهی قیمت‌ها. تغییرات: {string.Join("; ", changesLog)}. خطاها: {string.Join("; ", errors)}",
            ct);

        return ServiceResult.Success();
    }
}