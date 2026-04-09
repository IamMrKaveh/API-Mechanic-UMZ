using Domain.Shipping.Interfaces;
using Domain.Variant.Interfaces;

namespace Application.Variant.Features.Commands.UpdateProductVariantShipping;

public class UpdateProductVariantShippingHandler(
    IVariantRepository variantRepository,
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ILogger<UpdateProductVariantShippingHandler> logger) : IRequestHandler<UpdateVariantShippingCommand, ServiceResult>
{
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly IShippingRepository _shippingRepository = shippingRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<UpdateProductVariantShippingHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        UpdateVariantShippingCommand request,
        CancellationToken ct)
    {
        var variant = await _variantRepository.GetByIdForUpdateAsync(request.VariantId, ct);
        if (variant == null)
            return ServiceResult.NotFound("محصول یافت نشد.");

        var validShippingIds = await _shippingRepository.GetAllAsync(false, ct);
        var validIds = validShippingIds.Select(sm => sm.Id).ToHashSet();

        var invalidIds = request.EnabledShippingIds.Where(id => !validIds.Contains(id)).ToList();
        if (invalidIds.Any())
        {
            return ServiceResult.Unexpected($"برخی از روش‌های ارسال انتخاب شده نامعتبر هستند: {string.Join(", ", invalidIds)}");
        }

        variant.UpdateDetails(variant.Sku, request.ShippingMultiplier);

        var existings = variant.ProductVariantShippings.ToList();
        var sToRemove = existings
            .Where(x => !request.EnabledShippingIds.Contains(x.ShippingId))
            .ToList();

        foreach (var item in sToRemove)
        {
            variant.RemoveShipping(item.ShippingId);
        }

        var existingIds = existings.Select(x => x.ShippingId).ToHashSet();
        var idsToAdd = request.EnabledShippingIds.Where(id => !existingIds.Contains(id)).ToList();

        if (idsToAdd.Any())
        {
            var shippingsToAdd = await _shippingRepository.GetByIdsAsync(idsToAdd, ct);
            foreach (var sm in shippingsToAdd)
            {
                variant.AddShipping(sm);
            }
        }

        _variantRepository.Update(variant);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            variant.ProductId,
            "UpdateShippings",
            $"Shipping s updated for variant {request.VariantId}.",
            request.UserId);

        return ServiceResult.Success();
    }
}