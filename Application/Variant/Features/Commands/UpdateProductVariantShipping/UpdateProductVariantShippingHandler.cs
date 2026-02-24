namespace Application.Variant.Features.Commands.UpdateProductVariantShipping;

public class UpdateProductVariantShippingHandler : IRequestHandler<UpdateProductVariantShippingCommand, ServiceResult>
{
    private readonly IVariantRepository _variantRepository;
    private readonly IShippingRepository _shippingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateProductVariantShippingHandler> _logger;

    public UpdateProductVariantShippingHandler(
        IVariantRepository variantRepository,
        IShippingRepository shippingRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<UpdateProductVariantShippingHandler> logger)
    {
        _variantRepository = variantRepository;
        _shippingRepository = shippingRepository;
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

        var validShippingIds = await _shippingRepository.GetAllAsync(false, cancellationToken);
        var validIds = validShippingIds.Select(sm => sm.Id).ToHashSet();

        var invalidIds = request.EnabledShippingIds.Where(id => !validIds.Contains(id)).ToList();
        if (invalidIds.Any())
        {
            return ServiceResult.Failure($"برخی از روش‌های ارسال انتخاب شده نامعتبر هستند: {string.Join(", ", invalidIds)}");
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
            var shippingsToAdd = await _shippingRepository.GetByIdsAsync(idsToAdd, cancellationToken);
            foreach (var sm in shippingsToAdd)
            {
                variant.AddShipping(sm);
            }
        }

        _variantRepository.Update(variant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogProductEventAsync(variant.ProductId, "UpdateShippings", $"Shipping s updated for variant {request.VariantId}.", request.UserId);

        return ServiceResult.Success();
    }
}