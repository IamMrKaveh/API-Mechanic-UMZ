using Application.Common.Interfaces;
using Application.Product.Features.Shared;
using Domain.Attribute.Entities;
using Domain.Attribute.Interfaces;
using Domain.Common.Exceptions;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
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
    public async Task<ServiceResult<ProductVariantViewDto>> Handle(
        AddVariantCommand request,
        CancellationToken ct)
    {
        return await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var productId = ProductId.From(request.ProductId);

                var product = await productRepository.GetByIdAsync(productId, ct);
                if (product is null)
                    return ServiceResult<ProductVariantViewDto>.NotFound("Product not found.");

                var attributeValues = request.AttributeValueIds.Count != 0
                    ? await attributeRepository.GetAttributeValuesByIdsAsync(request.AttributeValueIds, ct)
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

                if (request.EnabledShippingIds is not null && request.EnabledShippingIds.Count != 0)
                {
                    var shippingMethods = await shippingMethodRepository.GetByIdsAsync(request.EnabledShippingIds, ct);
                    foreach (var sm in shippingMethods)
                    {
                        product.AddVariantShippingMethod(variant.Id, sm);
                    }
                }

                productRepository.Update(product);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                await auditService.LogProductEventAsync(
                    product.Id,
                    "AddVariant",
                    $"Variant added to product '{product.Name}'. SKU: {variant.Sku}",
                    currentUserService.CurrentUser.UserId);

                var variants = await productQueryService.GetProductVariantsAsync(product.Id, false, ct);
                var result = variants.FirstOrDefault(v => v.Id == variant.Id);

                return ServiceResult<ProductVariantViewDto>.Success(result!);
            }
            catch (DomainException ex)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return ServiceResult<ProductVariantViewDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                logger.LogError(ex, "Error occurred while adding variant to product {ProductId}", request.ProductId);
                return ServiceResult<ProductVariantViewDto>.Failure("An error occurred while adding the variant.");
            }
        }, ct);
    }
}