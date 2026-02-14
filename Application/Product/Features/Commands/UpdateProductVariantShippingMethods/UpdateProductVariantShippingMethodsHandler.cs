namespace Application.Product.Features.Commands.UpdateProductVariantShippingMethods;

public class UpdateProductVariantShippingMethodsHandler : IRequestHandler<UpdateProductVariantShippingMethodsCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateProductVariantShippingMethodsHandler> _logger;

    public UpdateProductVariantShippingMethodsHandler(
        IProductRepository productRepository,
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<UpdateProductVariantShippingMethodsHandler> logger)
    {
        _productRepository = productRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(UpdateProductVariantShippingMethodsCommand request, CancellationToken cancellationToken)
    {
        var variant = await _productRepository.GetVariantByIdForUpdateAsync(request.VariantId);
        if (variant == null)
        {
            return ServiceResult.Failure("محصول یافت نشد.");
        }

        var validShippingMethodIds = await _shippingMethodRepository.GetAllAsync(false);
        var validIds = validShippingMethodIds.Select(sm => sm.Id).ToHashSet();

        var invalidIds = request.EnabledShippingMethodIds.Where(id => !validIds.Contains(id)).ToList();
        if (invalidIds.Any())
        {
            return ServiceResult.Failure($"برخی از روش‌های ارسال انتخاب شده نامعتبر هستند: {string.Join(", ", invalidIds)}");
        }

        variant.UpdateDetails(variant.Sku, request.ShippingMultiplier);
        // Update collection manually since it's a join entity usually
        var existingMethods = variant.ProductVariantShippingMethods.ToList();
        var methodsToRemove = existingMethods
            .Where(x => !request.EnabledShippingMethodIds.Contains(x.ShippingMethodId))
            .ToList();

        foreach (var item in methodsToRemove)
        {
            variant.RemoveShippingMethod(item.ShippingMethodId);
        }

        var existingIds = existingMethods.Select(x => x.ShippingMethodId).ToHashSet();
        var idsToAdd = request.EnabledShippingMethodIds.Where(id => !existingIds.Contains(id)).ToList();

        // We need to load shipping methods entities to add them
        if (idsToAdd.Any())
        {
            var shippingMethodsToAdd = await _shippingMethodRepository.GetByIdsAsync(idsToAdd, cancellationToken);
            foreach (var sm in shippingMethodsToAdd)
            {
                variant.AddShippingMethod(sm);
            }
        }

        _productRepository.UpdateVariant(variant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogProductEventAsync(variant.ProductId, "UpdateShippingMethods", $"Shipping methods updated for variant {request.VariantId}.", request.UserId);

        return ServiceResult.Success();
    }
}