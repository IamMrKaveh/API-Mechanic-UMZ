using Application.Variant.Features.Shared;
using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.Inventory.Interfaces;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Application.Variant.Features.Commands.AddVariant;

public sealed class AddVariantHandler(
    IProductRepository productRepository,
    IVariantRepository variantRepository,
    IInventoryRepository inventoryRepository,
    IAttributeRepository attributeRepository,
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ILogger<AddVariantHandler> logger)
    : ICommandHandler<AddVariantCommand, ProductVariantViewDto>
{
    public async Task<ServiceResult<ProductVariantViewDto>> Handle(
        AddVariantCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        if (currentUserService.UserId is null)
            return ServiceResult<ProductVariantViewDto>.Unauthorized();

        var userId = UserId.From(currentUserService.UserId.Value);

        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult<ProductVariantViewDto>.NotFound("محصول یافت نشد.");

        var attributeValues = request.AttributeValueIds.Count != 0
            ? await attributeRepository.GetAttributeValuesByIdsAsync(
                request.AttributeValueIds.Select(AttributeValueId.From), ct)
            : [];

        if (request.AttributeValueIds.Count != 0)
        {
            var foundIds = attributeValues.Select(av => av.Id.Value).ToHashSet();
            var missingIds = request.AttributeValueIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Count != 0)
                return ServiceResult<ProductVariantViewDto>.Validation(
                    $"شناسه‌های ویژگی نامعتبر: {string.Join(", ", missingIds)}");

            var duplicateTypes = attributeValues
                .GroupBy(av => av.AttributeTypeId)
                .Any(g => g.Count() > 1);
            if (duplicateTypes)
                return ServiceResult<ProductVariantViewDto>.Validation(
                    "برای هر نوع ویژگی فقط یک مقدار مجاز است.");

            var signature = attributeValues.Select(av => av.Id.Value).OrderBy(id => id).ToList();
            var duplicateVariantExists = await variantRepository.ExistsByAttributeCombinationAsync(
                productId, signature, excludeId: null, ct);
            if (duplicateVariantExists)
                return ServiceResult<ProductVariantViewDto>.Conflict(
                    "تنوعی با همین ترکیب ویژگی‌ها از قبل وجود دارد.");
        }

        Sku sku;
        try
        {
            sku = request.Sku is not null
                ? Sku.Create(request.Sku)
                : Sku.Create(Guid.NewGuid().ToString("N")[..12].ToUpperInvariant());
        }
        catch (DomainException ex)
        {
            return ServiceResult<ProductVariantViewDto>.Validation(ex.Message);
        }

        var skuExists = await variantRepository.ExistsBySkuAsync(sku, null, ct);
        if (skuExists)
            return ServiceResult<ProductVariantViewDto>.Conflict("این SKU قبلاً استفاده شده است.");

        if (request.OriginalPrice < request.SellingPrice)
            return ServiceResult<ProductVariantViewDto>.Validation(
                "قیمت اصلی نمی‌تواند کمتر از قیمت فروش باشد.");

        List<Domain.Shipping.Aggregates.Shipping> shippings = [];
        if (request.EnabledShippingIds is { Count: > 0 })
        {
            var shippingIds = request.EnabledShippingIds.Select(ShippingId.From).ToList();
            shippings = (await shippingRepository.GetByIdsAsync(shippingIds, ct)).ToList();

            var foundShippingIds = shippings.Select(s => s.Id.Value).ToHashSet();
            var missingShipping = request.EnabledShippingIds.Where(id => !foundShippingIds.Contains(id)).ToList();
            if (missingShipping.Count != 0)
                return ServiceResult<ProductVariantViewDto>.Validation(
                    $"روش‌های ارسال نامعتبر: {string.Join(", ", missingShipping)}");
        }

        var variantId = VariantId.NewId();

        ProductVariant variant;
        try
        {
            var sellingPrice = Money.FromDecimal(request.SellingPrice);
            var originalPrice = request.OriginalPrice > request.SellingPrice
                ? Money.FromDecimal(request.OriginalPrice)
                : null;

            variant = ProductVariant.Create(variantId, productId, sku, sellingPrice, originalPrice);

            if (attributeValues.Any())
            {
                var assignments = attributeValues.Select(av =>
                    AttributeAssignment.Create(av.AttributeTypeId, av.Id, av.Value));
                variant.SetAttributes(assignments);
            }

            if (shippings.Count > 0)
            {
                var multiplier = request.ShippingMultiplier <= 0 ? 1m : request.ShippingMultiplier;
                variant.SetShippingMethods(
                    multiplier,
                    shippings.Select(s => new ShippingAssignment(s.Id, 0, 0, 0, 0)));
            }
        }
        catch (DomainException ex)
        {
            return ServiceResult<ProductVariantViewDto>.Validation(ex.Message);
        }

        var inventoryQuantity = request.IsUnlimited ? 0 : Math.Max(0, request.Stock);
        var inventory = Domain.Inventory.Aggregates.Inventory.Create(
            variantId,
            inventoryQuantity,
            request.IsUnlimited,
            lowStockThreshold: 5,
            createdBy: userId);

        try
        {
            await unitOfWork.ExecuteStrategyAsync(async cancellationToken =>
            {
                await variantRepository.AddAsync(variant, cancellationToken);
                await inventoryRepository.AddAsync(inventory, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return true;
            }, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            logger.LogWarning(ex, "Unique constraint violation while adding variant for product {ProductId}", productId.Value);
            return ServiceResult<ProductVariantViewDto>.Conflict("تنوع تکراری: SKU یا ترکیب ویژگی‌ها از قبل وجود دارد.");
        }
        catch (DBConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while adding variant for product {ProductId}", productId.Value);
            return ServiceResult<ProductVariantViewDto>.Conflict("تغییرات همزمان رخ داده است. لطفاً دوباره تلاش کنید.");
        }

        await auditService.LogProductEventAsync(
            product.Id,
            "AddVariant",
            $"واریانت به محصول '{product.Name}' اضافه شد. SKU: {sku.Value}",
            userId);

        var dto = ProductVariantViewDtoFactory.Create(variant, inventory, [.. attributeValues]);
        return ServiceResult<ProductVariantViewDto>.Success(dto);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == "23505";
}