namespace Application.Product.Features.Commands.ChangePrice;

public class ChangePriceHandler : IRequestHandler<ChangePriceCommand, ServiceResult>
{
    private readonly IVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public ChangePriceHandler(
        IVariantRepository variantRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult> Handle(ChangePriceCommand request, CancellationToken ct)
    {
        var variant = await _variantRepository.GetByIdAsync(request.VariantId, ct);
        if (variant == null || variant.ProductId != request.ProductId)
            return ServiceResult.Failure("Variant not found.");

        try
        {
            variant.SetPricing(request.PurchasePrice, request.SellingPrice, request.OriginalPrice);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        _variantRepository.Update(variant);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            request.ProductId, "ChangePrice",
            $"Variant {request.VariantId} prices changed. Selling: {request.SellingPrice}, Original: {request.OriginalPrice}",
            _currentUserService.UserId);

        await _cacheService.ClearAsync($"product:{request.ProductId}");
        await _cacheService.ClearAsync($"variant:{request.VariantId}");

        return ServiceResult.Success();
    }
}