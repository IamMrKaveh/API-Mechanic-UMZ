using Application.Audit.Contracts;
using Application.Security.Contracts;

namespace Application.Product.Features.Commands.BulkUpdatePrices;

public class BulkUpdatePricesHandler : IRequestHandler<BulkUpdatePricesCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BulkUpdatePricesHandler> _logger;

    public BulkUpdatePricesHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        ILogger<BulkUpdatePricesHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(BulkUpdatePricesCommand request, CancellationToken ct)
    {
        // Group updates by product to load each aggregate once
        var grouped = request.Updates.GroupBy(u => u.ProductId);
        var errors = new List<string>();
        var affectedProductIds = new HashSet<int>();
        var changesLog = new List<string>();

        foreach (var group in grouped)
        {
            var product = await _productRepository.GetByIdWithVariantsAsync(group.Key, ct);
            if (product == null)
            {
                errors.Add($"Product {group.Key} not found.");
                continue;
            }

            foreach (var update in group)
            {
                try
                {
                    product.ChangeVariantPrices(
                        update.VariantId,
                        update.PurchasePrice,
                        update.SellingPrice,
                        update.OriginalPrice);

                    changesLog.Add($"Variant {update.VariantId}: Selling={update.SellingPrice}");
                    affectedProductIds.Add(product.Id);
                }
                catch (DomainException ex)
                {
                    errors.Add($"Variant {update.VariantId}: {ex.Message}");
                }
            }

            _productRepository.Update(product);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Clear cache for affected products
        foreach (var productId in affectedProductIds)
        {
            await _cacheService.ClearAsync($"product:{productId}");
        }

        await _auditService.LogSystemEventAsync(
            "BulkPriceUpdate",
            $"User {_currentUserService.UserId} updated prices. Changes: {string.Join("; ", changesLog)}. Errors: {string.Join("; ", errors)}",
            _currentUserService.UserId);

        if (errors.Any())
        {
            _logger.LogWarning("Bulk price update had errors: {Errors}", string.Join("; ", errors));
        }

        return ServiceResult.Success();
    }
}