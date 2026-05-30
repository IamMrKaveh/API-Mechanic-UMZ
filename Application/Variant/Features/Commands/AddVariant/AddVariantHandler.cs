using Application.Variant.Features.Shared;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Commands.AddVariant;

public class AddVariantHandler(
    IProductRepository productRepository,
    IVariantRepository variantRepository,
    IAttributeRepository attributeRepository,
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork,
    IVariantQueryService variantQueryService,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<AddVariantCommand, ServiceResult<ProductVariantViewDto>>
{
    public async Task<ServiceResult<ProductVariantViewDto>> Handle(
        AddVariantCommand request,
        CancellationToken ct)
    {
        try
        {
            return await unitOfWork.ExecuteStrategyAsync(async (_, cancellationToken) =>
            {
                var productId = ProductId.From(request.ProductId);
                var userId = UserId.From(currentUserService.CurrentUser.UserId);

                var product = await productRepository.GetByIdAsync(productId, cancellationToken);
                if (product is null)
                    return ServiceResult<ProductVariantViewDto>.NotFound("محصول یافت نشد.");

                var attributeValueIds = request.AttributeValueIds.Select(AttributeValueId.From);
                var attributeValues = request.AttributeValueIds.Count != 0
                    ? await attributeRepository.GetAttributeValuesByIdsAsync(attributeValueIds, cancellationToken)
                    : [];

                if (request.AttributeValueIds.Count != 0)
                {
                    var missingIds = request.AttributeValueIds
                        .Except(attributeValues.Select(av => av.Id.Value))
                        .ToList();
                    if (missingIds.Count != 0)
                        return ServiceResult<ProductVariantViewDto>.Validation(
                            $"شناسه‌های ویژگی نامعتبر: {string.Join(", ", missingIds)}");
                }

                var variantId = VariantId.NewId();
                var sku = request.Sku is not null
                    ? Sku.Create(request.Sku)
                    : Sku.Create(Guid.NewGuid().ToString("N")[..12]);
                var price = Money.FromDecimal(request.SellingPrice);
                var compareAtPrice = request.OriginalPrice > request.SellingPrice
                    ? Money.FromDecimal(request.OriginalPrice)
                    : null;

                var variant = ProductVariant.Create(variantId, productId, sku, price, compareAtPrice);

                if (request.AttributeValueIds.Count != 0)
                {
                    var assignments = attributeValues.Select(av =>
                        AttributeAssignment.Create(av.AttributeTypeId, av.Id, av.Value));
                    variant.SetAttributes(assignments);
                }

                if (request.EnabledShippingIds is not null && request.EnabledShippingIds.Count != 0)
                {
                    var shippingIds = request.EnabledShippingIds.Select(ShippingId.From);
                    var shippings = await shippingRepository.GetByIdsAsync(shippingIds, cancellationToken);
                    var shippingAssignments = shippings.Select(s =>
                        new ShippingAssignment(s.Id, 0, 0, 0, 0));
                    variant.SetShippingMethods(shippingAssignments);
                }

                await variantRepository.AddAsync(variant, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                await auditService.LogProductEventAsync(
                    product.Id,
                    "AddVariant",
                    $"واریانت به محصول '{product.Name}' اضافه شد. SKU: {sku.Value}",
                    userId);

                var variants = await variantQueryService.GetProductVariantsAsync(productId, false, cancellationToken);
                var result = variants.FirstOrDefault(v => v.Id == variantId.Value);

                return ServiceResult<ProductVariantViewDto>.Success(result!);
            }, ct);
        }
        catch (DomainException ex)
        {
            return ServiceResult<ProductVariantViewDto>.Failure(ex.Message);
        }
        catch (Exception)
        {
            return ServiceResult<ProductVariantViewDto>.Failure("خطایی در افزودن واریانت رخ داد.");
        }
    }
}