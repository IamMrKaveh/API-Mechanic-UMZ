using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Variant.Interfaces;
using SharedKernel.Contracts;

namespace Application.Product.Features.Commands.ChangePrice;

public class ChangePriceHandler(
    IVariantRepository variantRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ICacheService cacheService) : IRequestHandler<ChangePriceCommand, ServiceResult>
{
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ICacheService _cacheService = cacheService;

    public async Task<ServiceResult> Handle(
        ChangePriceCommand request,
        CancellationToken ct)
    {
        var variant = await _variantRepository.GetByIdAsync(request.VariantId, ct);
        if (variant == null || variant.ProductId != request.ProductId)
            return ServiceResult.NotFound("Variant not found.");

        try
        {
            variant.SetPricing(request.PurchasePrice, request.SellingPrice, request.OriginalPrice);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }

        _variantRepository.Update(variant);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            request.ProductId, "ChangePrice",
            $"Variant {request.VariantId} prices changed. Selling: {request.SellingPrice}, Original: {request.OriginalPrice}",
            _currentUserService.CurrentUser.UserId);

        await _cacheService.ClearAsync($"product:{request.ProductId}");
        await _cacheService.ClearAsync($"variant:{request.VariantId}");

        return ServiceResult.Success();
    }
}