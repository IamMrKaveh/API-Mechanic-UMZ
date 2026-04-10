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
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ICacheService _cacheService = cacheService;
    private readonly ILogger<BulkUpdatePricesHandler> _logger = logger;

    public async Task<ServiceResult> Handle(BulkUpdatePricesCommand request, CancellationToken ct)
    {
        var updatesDict = request.Updates.ToDictionary(u => u.VariantId);
        var variants = await _variantRepository.GetByIdsAsync(updatesDict.Keys, ct);

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
                    _variantRepository.Update(variant);

                    changesLog.Add($"Variant {variant.Id}: Selling={update.SellingPrice}");
                    affectedProductIds.Add(variant.ProductId);
                }
                catch (DomainException ex)
                {
                    errors.Add($"Variant {variant.Id}: {ex.Message}");
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        foreach (var productId in affectedProductIds)
        {
            await _cacheService.ClearAsync($"product:{productId}");
        }

        await _auditService.LogSystemEventAsync(
            "BulkPriceUpdate",
            $"User {_currentUserService.UserId} updated prices. Changes: {string.Join("; ", changesLog)}. Errors: {string.Join("; ", errors)}",
            _currentUserService.UserId);

        if (errors.Count != 0)
        {
            _logger.LogWarning("Bulk price update had errors: {Errors}", string.Join("; ", errors));
        }

        return ServiceResult.Success();
    }
}