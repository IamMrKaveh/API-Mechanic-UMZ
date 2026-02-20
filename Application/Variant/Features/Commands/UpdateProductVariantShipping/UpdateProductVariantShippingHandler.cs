using Application.Shipping.Contracts;

namespace Application.Variant.Features.Commands.UpdateProductVariantShipping;

public class UpdateProductVariantShippingHandler : IRequestHandler<UpdateProductVariantShippingCommand, ServiceResult>
{
    private readonly IVariantRepository _variantRepository;
    private readonly IShippingRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateProductVariantShippingHandler> _logger;

    public UpdateProductVariantShippingHandler(
        IVariantRepository variantRepository,
        IShippingRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<UpdateProductVariantShippingHandler> logger)
    {
        _variantRepository = variantRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(UpdateProductVariantShippingCommand request, CancellationToken cancellationToken)
    {
        var variant = await _variantRepository.GetByIdForUpdateAsync(request.VariantId, cancellationToken);
        if (variant == null)
        {
            return ServiceResult.Failure("محصول یافت نشد.");
        }

        var validShippingMethodIds = await _shippingMethodRepository.GetAllAsync(false, cancellationToken);
        var validIds = validShippingMethodIds.Select(sm => sm.Id).ToHashSet();

        var invalidIds = request.EnabledShippingMethodIds.Where(id => !validIds.Contains(id)).ToList();
        if (invalidIds.Any())
        {
            return ServiceResult.Failure($"برخی از روش‌های ارسال انتخاب شده نامعتبر هستند: {string.Join(", ", invalidIds)}");
        }

        variant.UpdateDetails(variant.Sku, request.ShippingMultiplier);

        var existingMethods = variant.ProductVariantShippingMethods.ToList();
        var methodsToRemove = existingMethods
            .Where(x => !request.EnabledShippingMethodIds.Contains(x.ShippingId))
            .ToList();

        foreach (var item in methodsToRemove)
        {
            variant.RemoveShipping(item.ShippingId);
        }

        var existingIds = existingMethods.Select(x => x.ShippingId).ToHashSet();
        var idsToAdd = request.EnabledShippingMethodIds.Where(id => !existingIds.Contains(id)).ToList();

        if (idsToAdd.Any())
        {
            var shippingMethodsToAdd = await _shippingMethodRepository.GetByIdsAsync(idsToAdd, cancellationToken);
            foreach (var sm in shippingMethodsToAdd)
            {
                variant.AddShipping(sm);
            }
        }

        _variantRepository.Update(variant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogProductEventAsync(variant.ProductId, "UpdateShippingMethods", $"Shipping methods updated for variant {request.VariantId}.", request.UserId);

        return ServiceResult.Success();
    }
}