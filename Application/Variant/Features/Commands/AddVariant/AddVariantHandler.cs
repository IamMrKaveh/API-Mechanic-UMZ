using Application.Common.Interfaces;
using Application.Product.Features.Shared;
using Domain.Attribute.Entities;
using Domain.Attribute.Interfaces;
using Domain.Common.Exceptions;
using Domain.Product.Interfaces;
using Domain.Shipping.Interfaces;

namespace Application.Variant.Features.Commands.AddVariant;

public class AddVariantHandler(
    IProductRepository productRepository,
    IAttributeRepository attributeRepository,
    IShippingRepository shippingMethodRepository,
    IUnitOfWork unitOfWork,
    IProductQueryService productQueryService,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ILogger<AddVariantHandler> logger) : IRequestHandler<AddVariantCommand, ServiceResult<ProductVariantViewDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IAttributeRepository _attributeRepository = attributeRepository;
    private readonly IShippingRepository _shippingMethodRepository = shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IProductQueryService _productQueryService = productQueryService;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<AddVariantHandler> _logger = logger;

    public async Task<ServiceResult<ProductVariantViewDto>> Handle(
        AddVariantCommand request,
        CancellationToken ct)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var product = await _productRepository.GetByIdWithVariantsAsync(request.ProductId, ct);
                if (product is null)
                    return ServiceResult<ProductVariantViewDto>.NotFound("Product not found.");

                var attributeValues = request.AttributeValueIds.Count != 0
                    ? await _attributeRepository.GetAttributeValuesByIdsAsync(request.AttributeValueIds, ct)
                    : new List<AttributeValue>();

                if (request.AttributeValueIds.Count != 0)
                {
                    var missingIds = request.AttributeValueIds.Except(attributeValues.Select(av => av.Id)).ToList();
                    if (missingIds.Any())
                        return ServiceResult<ProductVariantViewDto>.Validation($"Invalid attribute values: {string.Join(", ", missingIds)}");
                }

                var variant = product.AddVariant(
                    request.Sku,
                    request.PurchasePrice,
                    request.SellingPrice,
                    request.OriginalPrice,
                    request.Stock,
                    request.IsUnlimited,
                    request.ShippingMultiplier,
                    attributeValues);

                if (request.EnabledShippingMethodIds is not null && request.EnabledShippingMethodIds.Count != 0)
                {
                    var shippingMethods = await _shippingMethodRepository.GetByIdsAsync(request.EnabledShippingMethodIds, ct);
                    foreach (var sm in shippingMethods)
                    {
                        product.AddVariantShippingMethod(variant.Id, sm);
                    }
                }

                _productRepository.Update(product);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                await _auditService.LogProductEventAsync(
                    product.Id,
                    "AddVariant",
                    $"Variant added to product '{product.Name}'. SKU: {variant.Sku}",
                    _currentUserService.CurrentUser.UserId);

                var variants = await _productQueryService.GetProductVariantsAsync(product.Id, false, ct);
                var result = variants.FirstOrDefault(v => v.Id == variant.Id);

                return ServiceResult<ProductVariantViewDto>.Success(result!);
            }
            catch (DomainException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return ServiceResult<ProductVariantViewDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Error occurred while adding variant to product {ProductId}", request.ProductId);
                return ServiceResult<ProductVariantViewDto>.Failure("An error occurred while adding the variant.");
            }
        }, ct);
    }
}