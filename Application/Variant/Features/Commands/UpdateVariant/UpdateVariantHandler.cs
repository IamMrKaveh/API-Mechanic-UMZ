using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.Inventory.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Commands.UpdateVariant;

public class UpdateVariantHandler(
    IVariantRepository variantRepository,
    IInventoryRepository inventoryRepository,
    IAttributeRepository attributeRepository,
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateVariantCommand>
{
    public async Task<ServiceResult> Handle(
        UpdateVariantCommand request,
        CancellationToken ct)
    {
        if (currentUserService.UserId is null)
            return ServiceResult.Unauthorized();

        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(currentUserService.UserId.Value);
        var productId = ProductId.From(request.ProductId);

        var variant = await variantRepository.GetForUpdateAsync(variantId, ct);
        if (variant is null)
            return ServiceResult.NotFound("واریانت یافت نشد.");

        if (variant.ProductId != productId)
            return ServiceResult.Validation("واریانت متعلق به این محصول نیست.");

        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            var newSku = Sku.Create(request.Sku);
            if (!variant.Sku.Equals(newSku))
            {
                var skuExists = await variantRepository.ExistsBySkuAsync(newSku, variantId, ct);
                if (skuExists)
                    return ServiceResult.Conflict("این SKU قبلاً استفاده شده است.");
                variant.ChangeSku(newSku);
            }
        }

        var price = Money.FromDecimal(request.SellingPrice);
        var compareAtPrice = request.OriginalPrice > request.SellingPrice
            ? Money.FromDecimal(request.OriginalPrice)
            : null;

        try
        {
            variant.ChangePrice(price, compareAtPrice);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Validation(ex.Message);
        }

        if (request.AttributeValueIds is not null)
        {
            var attributeAssignmentResult = await BuildAttributeAssignmentsAsync(
                request.AttributeValueIds,
                productId,
                variantId,
                ct);

            if (!attributeAssignmentResult.IsSuccess)
                return attributeAssignmentResult.ToServiceResult();

            try
            {
                variant.SetAttributes(attributeAssignmentResult.Assignments);
            }
            catch (DomainException ex)
            {
                return ServiceResult.Validation(ex.Message);
            }
        }

        if (request.EnabledShippingIds is not null)
        {
            var multiplier = request.ShippingMultiplier <= 0 ? 1m : request.ShippingMultiplier;
            var newShippingIds = request.EnabledShippingIds.Select(ShippingId.From).ToList();

            var shippings = newShippingIds.Count != 0
                ? await shippingRepository.GetByIdsAsync(newShippingIds, ct)
                : Array.Empty<Domain.Shipping.Aggregates.Shipping>();

            var foundIds = shippings.Select(s => s.Id.Value).ToHashSet();
            var missing = request.EnabledShippingIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missing.Count != 0)
                return ServiceResult.Validation($"روش‌های ارسال نامعتبر: {string.Join(", ", missing)}");

            var shippingAssignments = shippings.Select(s =>
                new ShippingAssignment(s.Id, 0, 0, 0, 0));

            try
            {
                variant.SetShippingMethods(multiplier, shippingAssignments);
            }
            catch (DomainException ex)
            {
                return ServiceResult.Validation(ex.Message);
            }
        }

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
        {
            var initialStock = request.IsUnlimited ? 0 : Math.Max(0, request.Stock);
            inventory = Domain.Inventory.Aggregates.Inventory.Create(
                variantId,
                initialStock,
                request.IsUnlimited,
                lowStockThreshold: 5,
                createdBy: userId);
            await inventoryRepository.AddAsync(inventory, ct);
        }
        else
        {
            if (request.IsUnlimited)
            {
                inventory.SetUnlimited();
            }
            else
            {
                var currentStock = (int)inventory.StockQuantity;
                var stockDiff = request.Stock - currentStock;
                if (stockDiff > 0)
                    inventory.IncreaseStock(stockDiff, "به‌روزرسانی موجودی", userId);
                else if (stockDiff < 0)
                    inventory.DecreaseStock(Math.Abs(stockDiff), "به‌روزرسانی موجودی", userId);
            }
        }

        await unitOfWork.ExecuteStrategyAsync(async cancellationToken =>
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }, ct);

        await auditService.LogProductEventAsync(
            productId,
            "UpdateVariant",
            $"واریانت {variantId.Value} به‌روزرسانی شد.",
            userId);

        return ServiceResult.Success();
    }

    private async Task<AttributeAssignmentBuildResult> BuildAttributeAssignmentsAsync(
        ICollection<Guid> attributeValueIds,
        ProductId productId,
        VariantId variantId,
        CancellationToken ct)
    {
        if (attributeValueIds.Count == 0)
            return AttributeAssignmentBuildResult.Empty();

        var attributeValueIdVOs = attributeValueIds.Select(AttributeValueId.From).ToList();
        var attributeValues = (await attributeRepository.GetAttributeValuesByIdsAsync(attributeValueIdVOs, ct)).ToList();

        var missingIds = attributeValueIds
            .Except(attributeValues.Select(av => av.Id.Value))
            .ToList();
        if (missingIds.Count != 0)
            return AttributeAssignmentBuildResult.Failure($"شناسه‌های ویژگی نامعتبر: {string.Join(", ", missingIds)}");

        var duplicateTypes = attributeValues
            .GroupBy(av => av.AttributeTypeId)
            .Any(g => g.Count() > 1);
        if (duplicateTypes)
            return AttributeAssignmentBuildResult.ValidationFailure("برای هر نوع ویژگی فقط یک مقدار مجاز است.");

        var signature = attributeValues
            .Select(av => av.Id.Value)
            .OrderBy(id => id)
            .ToList();

        var duplicateVariantExists = await variantRepository.ExistsByAttributeCombinationAsync(
            productId,
            signature,
            excludeId: variantId,
            ct);

        if (duplicateVariantExists)
            return AttributeAssignmentBuildResult.Conflict("تنوع دیگری با همین ترکیب ویژگی‌ها از قبل وجود دارد.");

        var assignments = attributeValues.Select(av =>
            AttributeAssignment.Create(av.AttributeTypeId, av.Id, av.Value)).ToList();

        return AttributeAssignmentBuildResult.Success(assignments);
    }

    private sealed class AttributeAssignmentBuildResult
    {
        public bool IsSuccess { get; private init; }
        public IReadOnlyCollection<AttributeAssignment> Assignments { get; private init; } = Array.Empty<AttributeAssignment>();
        public string? ErrorMessage { get; private init; }
        public ServiceResultKind Kind { get; private init; }

        public static AttributeAssignmentBuildResult Empty() => new()
        {
            IsSuccess = true,
            Assignments = Array.Empty<AttributeAssignment>()
        };

        public static AttributeAssignmentBuildResult Success(IReadOnlyCollection<AttributeAssignment> assignments) => new()
        {
            IsSuccess = true,
            Assignments = assignments
        };

        public static AttributeAssignmentBuildResult Failure(string message) => new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            Kind = ServiceResultKind.Failure
        };

        public static AttributeAssignmentBuildResult ValidationFailure(string message) => new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            Kind = ServiceResultKind.Validation
        };

        public static AttributeAssignmentBuildResult Conflict(string message) => new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            Kind = ServiceResultKind.Conflict
        };

        public ServiceResult ToServiceResult() => Kind switch
        {
            ServiceResultKind.Validation => ServiceResult.Validation(ErrorMessage!),
            ServiceResultKind.Conflict => ServiceResult.Conflict(ErrorMessage!),
            _ => ServiceResult.Failure(ErrorMessage!)
        };

        public enum ServiceResultKind
        {
            Failure,
            Validation,
            Conflict
        }
    }
}