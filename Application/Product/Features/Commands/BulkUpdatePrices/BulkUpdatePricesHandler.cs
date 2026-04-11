using Application.Common.Interfaces;
using Domain.Common.Exceptions;
using Domain.Variant.Interfaces;

namespace Application.Product.Features.Commands.BulkUpdatePrices;

public class BulkUpdatePricesHandler(
    IVariantRepository variantRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ICacheService cacheService,
    ILogger<BulkUpdatePricesHandler> logger) : IRequestHandler<BulkUpdatePricesCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(BulkUpdatePricesCommand request, CancellationToken ct)
    {
        var updatesDict = request.Updates.ToDictionary(u => u.VariantId);
        var variants = await variantRepository.GetByIdsAsync(updatesDict.Keys, ct);

        var errors = new List<string>();
        var affectedProductIds = new HashSet<int>();
        var changesLog = new List<string>();

        foreach (var variant in variants)
        {
            if (updatesDict.TryGetValue(variant.Id, out var update))
            {
                try
                {
                    variant.SetPricing(update.PurchasePrice, update.SellingPrice, update.OriginalPrice);
                    variantRepository.Update(variant);

                    changesLog.Add($"Variant {variant.Id}: Selling={update.SellingPrice}");
                    affectedProductIds.Add(variant.ProductId);
                }
                catch (DomainException ex)
                {
                    errors.Add($"Variant {variant.Id}: {ex.Message}");
                }
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        foreach (var productId in affectedProductIds)
        {
            await cacheService.ClearAsync($"product:{productId}", ct);
        }

        await auditService.LogSystemEventAsync(
            "BulkPriceUpdate",
            $"User {currentUserService.UserId} updated prices. Changes: {string.Join("; ", changesLog)}. Errors: {string.Join("; ", errors)}",
            currentUserService.UserId);

        if (errors.Count != 0)
        {
            logger.LogWarning("Bulk price update had errors: {Errors}", string.Join("; ", errors));
        }

        return ServiceResult.Success();
    }
}